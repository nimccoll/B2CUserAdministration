//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C User Administration Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AADB2C.UserMigration.Models
{
    public class LocalAccountsModel
    {
        public string userType;
        public List<AccountModel> Users;

        public LocalAccountsModel()
        {
            Users = new List<AccountModel>();
        }

        /// <summary>
        /// Parse JSON string into UsersModel
        /// </summary>
        public static LocalAccountsModel Parse(string JSON)
        {
            return  JsonConvert.DeserializeObject(JSON, typeof(LocalAccountsModel)) as LocalAccountsModel;
        }
        /// <summary>
        /// Serialize the object into Json string
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    public class AccountModel
    {
        // Local account attributes
        public string signInName { set; get; }
        public string password { set; get; }

        // Social account attributes
        public string issuer { set; get; }
        public string issuerUserId { set; get; }

        // Local as social accont attributes
        public string email { set; get; }
        public string displayName { set; get; }
        public string firstName { set; get; }
        public string lastName { set; get; }
        public string extension_Organization { get; set; }
        public string extension_UserRole { get; set; }
    }
}
