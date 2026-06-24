using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models
{
    public class SessionLib
    {
        // private constructor
        private SessionLib()
        {
            SessionID = string.Empty;
            AdminID = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            UserName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Role = string.Empty;
            DateJoin = null;
            // Password REMOVED. Replaced with Bearer-token fields — SessionLib
            // never holds a plaintext/encrypted credential at rest anymore.
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
            AccessTokenExpires = null;
            RefreshTokenExpires = null;
            SubjectType = "admin";
        }
        public static SessionLib Current
        {
            get
            {
                SessionLib session =
                  (SessionLib)HttpContext.Current.Session["__MySession__"];
                if (session == null)
                {
                    session = new SessionLib();
                    HttpContext.Current.Session["__MySession__"] = session;
                }
                return session;
            }
        }
        public string SessionID { get; set; }
        public string AdminID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public Nullable<System.DateTime> DateJoin { get; set; }

        // Bearer-token replacements for the deleted Password field. AuthClient
        // reads/writes these on every authenticated upstream call (silent refresh
        // on 401, token rotation). The dashboard never displays these fields.
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public Nullable<System.DateTime> AccessTokenExpires { get; set; }
        public Nullable<System.DateTime> RefreshTokenExpires { get; set; }
        public string SubjectType { get; set; }   // "user" | "admin"
    }
}