using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models
{
    public class SessionLib
    {// private constructor
        private SessionLib()
        {
            SessionID = string.Empty;
            UserID = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
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
        public string UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public Nullable<System.DateTime> DateJoin { get; set; }
    }
}