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
            Password = string.Empty;
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
        public string Password { get; set; }
        public string Role { get; set; }
        public Nullable<System.DateTime> DateJoin { get; set; }
    }
}