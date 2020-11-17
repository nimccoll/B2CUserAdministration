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
using System.Collections.Generic;

namespace AADB2C.UserMigration.Models
{
    public class Document
    {
        public string id { get; set; }
        public string signInName { get; set; }
        public string issuer { get; set; }
        public string issuerId { get; set; }
        public string email { get; set; }
        public string displayName { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string organization { get; set; }
        public string userRole { get; set; }
    }

    public class SearchResults
    {
        public List<Document> value { get; set; }
    }
}