using System;
using System.Text.RegularExpressions;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Single source of truth for the account password policy, shared by every
    /// set-password entry point (registration, forgot-password reset, settings change,
    /// admin reset, admin invited-account). The previous rule was length-only (>= 8),
    /// which accepted weak passwords like "123412341234". This enforces:
    ///   - a minimum length (<see cref="MinLength"/>),
    ///   - at least 3 of 4 character classes (lowercase, uppercase, digit, symbol), and
    ///   - rejection of obviously weak passwords (common words, single-character or
    ///     short repeated patterns, and pure ascending/descending sequences).
    /// The SERVER check is authoritative; any client-side hint is advisory only.
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 12;

        // A short, high-signal list of common/guessable bases. Deliberately not exhaustive:
        // the structural checks (classes, repeats, sequences) carry most of the weight.
        private static readonly string[] CommonBases = new[]
        {
            "password", "passw0rd", "qwerty", "asdfgh", "zxcvbn", "letmein", "welcome",
            "admin", "administrator", "iloveyou", "monkey", "dragon", "sunshine",
            "princess", "football", "baseball", "abc123", "qwerty123", "111111",
            "123123", "000000", "1234", "12345", "123456", "1234567", "12345678",
            "qazwsx", "trustno1", "master", "login", "starwars", "whatever"
        };

        /// <summary>
        /// Returns true if the password satisfies the policy; otherwise false with a
        /// user-facing <paramref name="message"/> explaining the first failed rule.
        /// </summary>
        public static bool Validate(string password, out string message)
        {
            message = null;

            if (string.IsNullOrEmpty(password))
            {
                message = "Password cannot be empty.";
                return false;
            }
            if (password.Length < MinLength)
            {
                message = "Password must be at least " + MinLength + " characters long.";
                return false;
            }

            int classes = 0;
            if (Regex.IsMatch(password, "[a-z]")) classes++;
            if (Regex.IsMatch(password, "[A-Z]")) classes++;
            if (Regex.IsMatch(password, "[0-9]")) classes++;
            if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) classes++;
            if (classes < 3)
            {
                message = "Password must include at least 3 of: a lowercase letter, an uppercase letter, a number, and a symbol.";
                return false;
            }

            if (IsWeak(password))
            {
                message = "Password is too common or predictable. Choose something less guessable.";
                return false;
            }

            return true;
        }

        // Convenience overload for call sites that only need the boolean.
        public static bool IsValid(string password)
        {
            string ignored;
            return Validate(password, out ignored);
        }

        private static bool IsWeak(string password)
        {
            string lower = password.ToLowerInvariant();

            foreach (var b in CommonBases)
            {
                if (lower == b) return true;                                 // exactly a common base
                if (b.Length >= 7 && lower.Contains(b)) return true;         // a long common word anywhere in it
                if (lower.Contains(b) && (lower.Length - b.Length) <= 3) return true; // common base + light padding
            }

            if (IsSingleCharRepeated(password)) return true;   // "aaaaaaaaaaaa"
            if (IsShortBlockRepeated(password)) return true;   // "123412341234", "abababab..."
            if (IsMonotonicSequence(lower)) return true;       // "0123456789ab", "zyxwvutsrqpo"

            return false;
        }

        private static bool IsSingleCharRepeated(string s)
        {
            for (int i = 1; i < s.Length; i++)
                if (s[i] != s[0]) return false;
            return true;
        }

        // True if s is a block of length 1..4 repeated to fill the whole string.
        private static bool IsShortBlockRepeated(string s)
        {
            for (int len = 1; len <= 4 && len < s.Length; len++)
            {
                if (s.Length % len != 0) continue;
                string block = s.Substring(0, len);
                bool all = true;
                for (int i = len; i < s.Length; i += len)
                {
                    if (s.Substring(i, len) != block) { all = false; break; }
                }
                if (all) return true;
            }
            return false;
        }

        // True if every adjacent character steps by a constant +1 or -1 in code point
        // across the whole string (a pure sequence like "0123456789ab" or "zyxwvutsrqpo").
        private static bool IsMonotonicSequence(string s)
        {
            if (s.Length < 3) return false;
            int step = s[1] - s[0];
            if (step != 1 && step != -1) return false;
            for (int i = 2; i < s.Length; i++)
                if (s[i] - s[i - 1] != step) return false;
            return true;
        }
    }
}
