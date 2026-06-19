using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class AdminModel
    {
        public long ID { get; set; }
        public string AdminID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string RoleID { get; set; }
        public string Role { get; set; }
        public string PIN { get; set; }
        public Nullable<long> CountryID { get; set; }
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
        public string InvitedByFirstName { get; set; }
        public string InvitedByLastName { get; set; }
        public Nullable<System.DateTime> DateBanned { get; set; }
        public string BannedBy { get; set; }
        public string BannedByFirstName { get; set; }
        public string BannedByLastName { get; set; }
        public string RegisterVia { get; set; }
        public string ImageURL { get; set; }
        public string ThumbnailURL { get; set; }
    }
}