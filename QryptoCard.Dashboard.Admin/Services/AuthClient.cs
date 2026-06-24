using Newtonsoft.Json;
using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Models.Service;
using System;
using System.Net;
using System.Text;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Services
{
    // Silent-refresh-on-401 wrapper for upstream API calls. Replaces the
    // per-site Basic-auth header attach pattern (Security.credentials(...)) —
    // every Services/*Service.cs method now goes through ExecuteJsonPost (or
    // ExecuteJsonGet/Put/Delete) instead of constructing a WebClient directly.
    //
    // Token lifecycle:
    //   - login.aspx -> /v1/auth/login        (legacy, sends OTP)
    //   - otplogin.aspx -> MintAfterOtpVerify  (verifies OTP + mints token pair)
    //   - Each authenticated page load -> ExecuteJson* attaches
    //     "Bearer {AccessToken}". If a 401 comes back (token expired between
    //     pages), TryRefresh silently rotates and retries once.
    //   - logout.aspx -> Revoke (kills refresh-token chain server-side)
    //
    // Concurrent-tab refresh: TryRefresh holds the Session.SyncRoot lock while it
    // does the work (valid cross-request lock under InProc session mode). The
    // after-acquire re-check skips the network call when a sibling tab already
    // refreshed. Cross-process / cross-device races are NOT protected (different
    // SyncRoot instances); the server-side conditional UPDATE in refresh() handles
    // those by letting only one transaction win.
    public static class AuthClient
    {
        // 30s buffer: tokens "about to expire" within this window are treated as
        // expired so the next request silently refreshes rather than rolling the
        // dice on a 401.
        private const int ClockSkewBufferSeconds = 30;

        // ---------- public surface (called from service files) ----------

        public static OutputModel ExecuteJsonPost(string path, object body)
        {
            var json = body == null ? "" : JsonConvert.SerializeObject(body);
            return ExecuteWithRetry(path, "POST", json);
        }

        public static OutputModel ExecuteJsonPostString(string path, string jsonBody)
        {
            return ExecuteWithRetry(path, "POST", jsonBody ?? "");
        }

        public static OutputModel ExecuteJsonPut(string path, object body)
        {
            var json = body == null ? "" : JsonConvert.SerializeObject(body);
            return ExecuteWithRetry(path, "PUT", json);
        }

        public static OutputModel ExecuteJsonGet(string path)
        {
            return ExecuteWithRetry(path, "GET", null);
        }

        public static OutputModel ExecuteJsonDelete(string path, object body)
        {
            var json = body == null ? "" : JsonConvert.SerializeObject(body);
            return ExecuteWithRetry(path, "DELETE", json);
        }

        // In-place helper for any service-file pattern that still builds its own
        // WebClient. Each line that previously attached Basic auth via:
        //
        //   client.Headers[HttpRequestHeader.Authorization] =
        //       string.Format("Basic {0}",
        //           Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
        //
        // becomes simply:  AuthClient.AttachBearerHeader(client);
        //
        // Includes a pre-flight refresh check so requests with about-to-expire
        // tokens silently rotate before going out. No automatic retry on a
        // server-side mid-request revoke — use ExecuteJson* for that.
        public static void AttachBearerHeader(WebClient client)
        {
            if (IsAccessTokenExpired())
            {
                // Best-effort refresh. If it fails we still attach the (stale)
                // token and let the server reject; the caller's existing catch
                // block surfaces the 401 to the user.
                TryRefresh();
            }
            client.Headers[HttpRequestHeader.Authorization] =
                "Bearer " + (SessionLib.Current.AccessToken ?? "");
        }

        // ---------- login flow ----------

        // otplogin.aspx calls this after the user enters the OTP code. Populates
        // SessionLib token fields on success. Caller (otplogin.aspx.cs) also
        // deserializes the Profile field on the response into AdminModel and
        // populates the remaining SessionLib fields (Email, FirstName, etc.).
        public static OutputModel MintAfterOtpVerify(string otpSessionId, string otpCode)
        {
            return MintAtPath("/v1/auth/mint-after-otp", otpSessionId, otpCode);
        }

        static OutputModel MintAtPath(string path, string otpSessionId, string code)
        {
            try
            {
                Common.trustConnection();
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.Encoding = Encoding.UTF8;
                    var json = JsonConvert.SerializeObject(new
                    {
                        OtpSessionId = otpSessionId,
                        OtpCode      = code,
                        SubjectType  = SessionLib.Current.SubjectType
                    });
                    var responseStr = client.UploadString(
                        KeyModel.API_URL + path, "POST", json);
                    var op = JsonConvert.DeserializeObject<OutputModel>(responseStr);

                    if (op != null && op.Status == "success" && op.Data != null)
                    {
                        var resp = JsonConvert.DeserializeObject<AuthMintResponse>(op.Data.ToString());
                        ApplyTokensToSession(resp);
                        // op.Data is left as the raw JToken from the wire so that
                        // otplogin.aspx.cs can call op.Data.ToString() and get
                        // valid JSON for its own DeserializeObject call.
                    }
                    return op ?? new OutputModel { Status = "error", Message = "null response from " + path };
                }
            }
            catch (Exception ex)
            {
                return new OutputModel { Status = "error", Message = ex.ToString() };
            }
        }

        // ---------- logout flow ----------

        // logout.aspx calls this. Clears SessionLib tokens regardless of the
        // server response — local logout always proceeds. Server-side revoke is
        // best-effort: if the server is unreachable, the next attempted request
        // from any device still holding the refresh token will get a fresh access
        // token (until the refresh expires). Acceptable.
        public static OutputModel Revoke()
        {
            var refreshToken = SessionLib.Current.RefreshToken;
            try
            {
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    Common.trustConnection();
                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        client.Encoding = Encoding.UTF8;
                        var json = JsonConvert.SerializeObject(new { RefreshToken = refreshToken });
                        client.UploadString(KeyModel.API_URL + "/v1/auth/revoke", "POST", json);
                    }
                }
            }
            catch
            {
                // Ignored — local logout proceeds anyway.
            }
            ClearTokensInSession();
            return new OutputModel { Status = "success", Message = "ok" };
        }

        // ---------- internals ----------

        private static OutputModel ExecuteWithRetry(string path, string method, string json)
        {
            // Pre-flight: if the access token is past the expiry buffer, refresh
            // proactively. Failure is BEST-EFFORT — we still attempt the request
            // below with whatever token we have (or empty). This lets
            // unauthenticated endpoints (/v1/auth/login, /v1/auth/password/forgot,
            // etc.) succeed even when SessionLib has no tokens (pre-login state).
            if (IsAccessTokenExpired())
            {
                TryRefresh();   // best-effort; ignore failure
            }

            try
            {
                return ExecuteOnce(path, method, json);
            }
            catch (WebException wex) when (Is401(wex))
            {
                // Authenticated endpoint rejected our token (revoked / expired
                // between pre-flight check and request). One refresh + retry.
                if (!TryRefresh())
                {
                    return new OutputModel { Status = "error", Message = "Authentication required" };
                }
                try
                {
                    return ExecuteOnce(path, method, json);
                }
                catch (Exception ex)
                {
                    return new OutputModel { Status = "error", Message = ex.ToString() };
                }
            }
            catch (Exception ex)
            {
                return new OutputModel { Status = "error", Message = ex.ToString() };
            }
        }

        private static OutputModel ExecuteOnce(string path, string method, string json)
        {
            var responseStr = ExecuteOnceRaw(path, method, json);
            return JsonConvert.DeserializeObject<OutputModel>(responseStr)
                ?? new OutputModel { Status = "error", Message = "null response from " + path };
        }

        // Raw single-shot request (bearer attach + HTTP), returning the response
        // body unparsed. A 401 surfaces as the WebException the retry wrapper
        // catches.
        private static string ExecuteOnceRaw(string path, string method, string json)
        {
            Common.trustConnection();
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.Authorization] = "Bearer " + (SessionLib.Current.AccessToken ?? "");
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Encoding = Encoding.UTF8;
                var url = KeyModel.API_URL + path;
                if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    return client.DownloadString(url);
                }
                return client.UploadString(url, method, json ?? "");
            }
        }

        private static bool IsAccessTokenExpired()
        {
            if (string.IsNullOrEmpty(SessionLib.Current.AccessToken)) return true;
            if (!SessionLib.Current.AccessTokenExpires.HasValue) return true;
            return SessionLib.Current.AccessTokenExpires.Value <=
                   DateTime.UtcNow.AddSeconds(ClockSkewBufferSeconds);
        }

        // Concurrent-tab safe refresh. Acquires Session.SyncRoot, re-checks expiry
        // post-acquire (a sibling tab may have already refreshed), and only does
        // the actual refresh if still needed. Returns false on any failure —
        // caller treats as "Authentication required" and surfaces to the user
        // (typically: redirect to login.aspx).
        private static bool TryRefresh()
        {
            var session = HttpContext.Current?.Session;
            if (session == null) return false;

            lock (session.SyncRoot)
            {
                // After-acquire re-check: did a sibling tab already refresh?
                if (!IsAccessTokenExpired()) return true;

                var refreshToken = SessionLib.Current.RefreshToken;
                if (string.IsNullOrEmpty(refreshToken)) return false;

                try
                {
                    Common.trustConnection();
                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        client.Encoding = Encoding.UTF8;
                        var json = JsonConvert.SerializeObject(new { RefreshToken = refreshToken });
                        var responseStr = client.UploadString(
                            KeyModel.API_URL + "/v1/auth/refresh", "POST", json);
                        var op = JsonConvert.DeserializeObject<OutputModel>(responseStr);
                        if (op == null || op.Status != "success" || op.Data == null)
                        {
                            // Server rejected the refresh — token revoked, expired,
                            // or RTR reuse detection fired. Clear locally so the
                            // user is forced to re-login.
                            ClearTokensInSession();
                            return false;
                        }
                        var resp = JsonConvert.DeserializeObject<AuthMintResponse>(op.Data.ToString());
                        ApplyTokensToSession(resp);
                        return true;
                    }
                }
                catch
                {
                    // Network failure / 401 / serialization error — fail closed.
                    ClearTokensInSession();
                    return false;
                }
            }
        }

        private static void ApplyTokensToSession(AuthMintResponse resp)
        {
            if (resp == null) return;
            SessionLib.Current.AccessToken         = resp.AccessToken;
            SessionLib.Current.RefreshToken        = resp.RefreshToken;
            SessionLib.Current.AccessTokenExpires  = resp.AccessTokenExpires;
            SessionLib.Current.RefreshTokenExpires = resp.RefreshTokenExpires;
            if (!string.IsNullOrEmpty(resp.SubjectType))
                SessionLib.Current.SubjectType = resp.SubjectType;
        }

        private static void ClearTokensInSession()
        {
            SessionLib.Current.AccessToken         = string.Empty;
            SessionLib.Current.RefreshToken        = string.Empty;
            SessionLib.Current.AccessTokenExpires  = null;
            SessionLib.Current.RefreshTokenExpires = null;
        }

        private static bool Is401(WebException wex)
        {
            var resp = wex?.Response as HttpWebResponse;
            return resp != null && resp.StatusCode == HttpStatusCode.Unauthorized;
        }
    }
}
