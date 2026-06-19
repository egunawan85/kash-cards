using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Admin.Models.Service
{
    public class UserModel
    {
        public long ID { get; set; }
        public string UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Profession { get; set; }
        public Nullable<System.DateTime> DateJoin { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<System.DateTime> DateActivated { get; set; }
        public Nullable<int> isVerified { get; set; }
        public Nullable<System.DateTime> DateVerified { get; set; }
        public Nullable<int> isBanned { get; set; }
        public Nullable<System.DateTime> DateInvited { get; set; }
        public string InvitedBy { get; set; }
        public Nullable<System.DateTime> DateBanned { get; set; }
        public string BannedBy { get; set; }
        public string RegisterVia { get; set; }
        public string ImageURL { get; set; }
        public string ThumbnailURL { get; set; }
        public Nullable<int> is2FA { get; set; }
        public Nullable<System.DateTime> Date2FA { get; set; }
        public int TotalCard { get; set; }
    }
}