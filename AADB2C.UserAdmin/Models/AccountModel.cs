//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C User Management Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
namespace AADB2C.UserAdmin.Models
{
    public class AccountModel
    {
        public string objectId { get; set; }

        public string accountType { get; set; }

        // Local account attributes
        public string signInName { set; get; }
        public string password { set; get; }

        // Social account attributes
        public string issuer { set; get; }
        public string issuerUserId { set; get; }

        // Local as social account attributes
        public string email { set; get; }
        public string displayName { set; get; }
        public string firstName { set; get; }
        public string lastName { set; get; }
        public string extension_Organization { get; set; }
        public string extension_UserRole { get; set; }
    }
}