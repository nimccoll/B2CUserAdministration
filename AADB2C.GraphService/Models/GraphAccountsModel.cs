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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AADB2C.GraphService
{
    public class GraphAccounts
    {
        private static readonly string _extensionApplicationId = "{B2C Extension Application ID}";

        public string odatametadata { get; set; }

        public string odatanextLink { get; set; }

        public string skipToken
        {
            get
            {
                string value = string.Empty;
                if (!string.IsNullOrEmpty(odatanextLink))
                {
                    value = odatanextLink.Substring(odatanextLink.IndexOf("?") + 1);
                }
                return value;
            }
        }

        public List<GraphAccountModel> value { get; set; }

        public static GraphAccounts Parse(string JSON)
        {
            JSON = JSON.Replace("odata.metadata", "odatametadata");
            JSON = JSON.Replace("odata.nextLink", "odatanextLink");
            JSON = JSON.Replace($"extension_{_extensionApplicationId}_Organization", "extension_Organization");
            JSON = JSON.Replace($"extension_{_extensionApplicationId}_UserRole", "extension_UserRole");
            return JsonConvert.DeserializeObject(JSON, typeof(GraphAccounts)) as GraphAccounts;
        }
    }
}
