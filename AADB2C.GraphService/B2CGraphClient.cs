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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace AADB2C.GraphService
{
    public class B2CGraphClient
    {
        private AuthenticationContext authContext;
        private ClientCredential credential;
        static private AuthenticationResult AccessToken;

        public readonly string aadInstance = "https://login.microsoftonline.com/";
        public readonly string aadGraphResourceId = "https://graph.windows.net/";
        public readonly string aadGraphEndpoint = "https://graph.windows.net/";
        public readonly string aadGraphVersion = "api-version=1.6";

        public string Tenant { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }

        public B2CGraphClient(string tenant, string clientId, string clientSecret)
        {
            this.Tenant = tenant;
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;

            // The AuthenticationContext is ADAL's primary class, in which you indicate the direcotry to use.
            this.authContext = new AuthenticationContext("https://login.microsoftonline.com/" + this.Tenant);

            // The ClientCredential is where you pass in your client_id and client_secret, which are 
            // provided to Azure AD in order to receive an access_token using the app's identity.
            this.credential = new ClientCredential(this.ClientId, this.ClientSecret);
        }

        /// <summary>
        /// Create consumer user accounts
        /// When creating user accounts in a B2C tenant, you can send an HTTP POST request to the /users endpoint
        /// </summary>
        public async Task<GraphAccountModel> CreateAccount(
                                            string userType, 
                                            string signInName,
                                            string issuer,
                                            string issuerUserId,
                                            string email,
                                            string password, 
                                            string displayName, 
                                            string givenName, 
                                            string surname,
                                            string extension_Organization,
                                            string extension_UserRole,
                                            bool generateRandomPassword)
        {
            if (string.IsNullOrEmpty(signInName) && string.IsNullOrEmpty(issuerUserId ))
                throw new Exception("You must provide user's signInName or issuerUserId");

            if (string.IsNullOrEmpty(displayName) || displayName.Length < 1)
                throw new Exception("Dispay name is NULL or empty, you must provide valid dislay name");

            // Use random password for just-in-time migration flow
            if (generateRandomPassword)
            {
                password = GeneratePassword();
            }

            try
            {
                // Create Graph json string from object
                GraphAccountModel graphUserModel = new GraphAccountModel(
                                                Tenant,
                                                userType, 
                                                signInName,
                                                issuer,
                                                issuerUserId,
                                                email,
                                                password, 
                                                displayName, 
                                                givenName, 
                                                surname,
                                                extension_Organization,
                                                extension_UserRole);

                // Send the json to Graph API end point
                string JSON = await SendGraphRequest("/users/", null, graphUserModel.ToString(), HttpMethod.Post);
                GraphAccountModel newUser = GraphAccountModel.Parse(JSON);
                Console.WriteLine($"Azure AD user account '{displayName}' created");

                return newUser;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ObjectConflict"))
                {
                    // TBD: Add you error Handling here
                    Console.ForegroundColor = ConsoleColor.Red;

                    if (ex.Message.Contains("signInNames "))
                        Console.WriteLine($"User with same signInNames '{signInName}' already exists in Azure AD");
                    else if (ex.Message.Contains("userIdentities "))
                        Console.WriteLine($"User with same userIdentities '{issuerUserId}' already exists in Azure AD");
                    else if (ex.Message.Contains("one or more"))
                        Console.WriteLine($"User with same userIdentities '{issuerUserId}', and signInNames '{signInName}'  already exists in Azure AD");

                    Console.ResetColor();
                }

                return null;
            }
        }

        /// <summary>
        /// Search Azure AD user by signInNames property
        /// </summary>
        public async Task<string> SearchUserBySignInNames(string signInNames)
        {
            return await SendGraphRequest("/users/",
                            $"$filter=signInNames/any(x:x/value eq '{signInNames}')",
                            null, HttpMethod.Get);
        }

        /// <summary>
        /// Search Azure AD user by displayName property
        /// </summary>
        public async Task<string> SearchUserByDisplayName(string displayName)
        {
            return await SendGraphRequest("/users/",
                            $"$filter=displayName eq '{displayName}'",
                            null, HttpMethod.Get);
        }

        /// <summary>
        /// Update consumer user account's password
        /// </summary>
        /// <returns></returns>
        public async Task UpdateUserPassword(string signInName, string password)
        {
            string JSON = await this.SearchUserBySignInNames(signInName);

            GraphAccounts users = GraphAccounts.Parse(JSON);

            // If user exists
            if (users != null && users.value != null && users.value.Count == 1)
            {
                // Generate JSON containing the password and password policy
                GraphUserSetPasswordModel graphPasswordModel = new GraphUserSetPasswordModel(password);
                string json = JsonConvert.SerializeObject(graphPasswordModel);

                // Send the request to Graph API
                await SendGraphRequest("/users/" + users.value[0].objectId, null, json, new HttpMethod("PATCH"));
            }
        }

        /// <summary>
        /// Delete user anccounts from Azure AD by SignInName (email address)
        /// </summary>
        public async Task DeleteAADUserBySignInNames(string signInName)
        {
            // First step, get the user account ID
            string JSON = await this.SearchUserBySignInNames(signInName);

            GraphAccounts users = GraphAccounts.Parse(JSON);

            // If the user account Id return successfully, iterate through all accounts
            if (users != null && users.value != null && users.value.Count > 0)
            {
                foreach (var item in users.value)
                {
                    // Send delete request to Graph API
                    await SendGraphRequest("/users/" + item.objectId, null, null, HttpMethod.Delete);
                }
            }
        }

        public async Task UpdateUser(string objectId, string displayName, string givenName, string surname, string extension_Organization, string extension_UserRole)
        {
            GraphAccountModel user = await this.GetUserByObjectId(objectId);

            if (user != null)
            {
                GraphUserUpdateModel graphUserUpdateModel = new GraphUserUpdateModel(displayName, givenName, surname, extension_Organization, extension_UserRole);
                string json = graphUserUpdateModel.ToString();
                await SendGraphRequest($"/users/{user.objectId}", null, json, new HttpMethod("PATCH"));
            }
        }

        public async Task<GraphAccountModel> GetUser(string signInName)
        {
            GraphAccountModel graphAccountModel = null;
            string JSON = await this.SearchUserBySignInNames(signInName);

            GraphAccounts users = GraphAccounts.Parse(JSON);

            // If user exists
            if (users != null && users.value != null && users.value.Count == 1)
            {
                string user = await SendGraphRequest($"/users/{users.value[0].objectId}", null, null, HttpMethod.Get);
                graphAccountModel = GraphAccountModel.Parse(user);
            }

            return graphAccountModel;
        }

        public async Task<GraphAccountModel> GetUserByUserPrincipalName(string userPrincipalName)
        {
            GraphAccountModel graphAccountModel = null;
            string JSON = await SendGraphRequest("/users/",
                            $"$filter=userPrincipalName eq '{userPrincipalName}'",
                            null, HttpMethod.Get);

            GraphAccounts users = GraphAccounts.Parse(JSON);

            // If user exists
            if (users != null && users.value != null && users.value.Count == 1)
            {
                string user = await SendGraphRequest($"/users/{users.value[0].objectId}", null, null, HttpMethod.Get);
                graphAccountModel = GraphAccountModel.Parse(user);
            }

            return graphAccountModel;

        }

        public async Task<GraphAccountModel> GetUserByObjectId(string objectId)
        {
            GraphAccountModel graphAccountModel = null;

            string user = await SendGraphRequest($"/users/{objectId}", null, null, HttpMethod.Get);
            graphAccountModel = GraphAccountModel.Parse(user);

            return graphAccountModel;
        }

        public async Task<string> ListUsers(int pageSize = 100, string skipToken = "")
        {
            string result = string.Empty;
            string queryString = $"$top={pageSize}";
            if (!string.IsNullOrEmpty(skipToken))
            {
                queryString = $"{queryString}&{skipToken}";
            }
            try
            {
                result = await SendGraphRequest("/users/",
                                queryString,
                                null, HttpMethod.Get);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid previous page search request"))
                {
                    return "end of results";
                }
                else
                {
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Handle Graph user API, support following HTTP methods: GET, POST and PATCH
        /// </summary>
        private async Task<string> SendGraphRequest(string api, string query, string data, HttpMethod method)
        {
            // Get the access toke to Graph API
            string acceeToken = await AcquireAccessToken();

            // Set the Graph url. Including: Graph-endpoint/tenat/users?api-version&query
            string url = $"{this.aadGraphEndpoint}{this.Tenant}{api}?{this.aadGraphVersion}";

            if (!string.IsNullOrEmpty(query))
            {
                url += "&" + query;
            }

            //Trace.WriteLine($"Graph API call: {url}");
            try
            {
                using (HttpClient http = new HttpClient())
                using (HttpRequestMessage request = new HttpRequestMessage(method, url))
                {
                    // Set the authorization header
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceeToken);

                    // For POST and PATCH set the request content 
                    if (!string.IsNullOrEmpty(data))
                    {
                        //Trace.WriteLine($"Graph API data: {data}");
                        request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                    }

                    // Send the request to Graph API endpoint
                    using (HttpResponseMessage response = await http.SendAsync(request))
                    {
                        string error = await response.Content.ReadAsStringAsync();

                        // Check the result for error
                        if (!response.IsSuccessStatusCode)
                        {
                            // Throw server busy error message
                            if (response.StatusCode == (HttpStatusCode)429)
                            {
                                // TBD: Add you error handling here
                            }

                            throw new Exception(error);
                        }

                        // Return the response body, usually in JSON format
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception)
            {
                // TBD: Add you error handling here
                throw;
            }
        }

        private async Task<string> AcquireAccessToken()
        {
            // If the access token is null or about to be invalid, acquire new one
            if (B2CGraphClient.AccessToken == null ||
                (B2CGraphClient.AccessToken.ExpiresOn.UtcDateTime > DateTime.UtcNow.AddMinutes(-10)))
            {
                try
                {
                    B2CGraphClient.AccessToken = await authContext.AcquireTokenAsync(this.aadGraphResourceId, credential);
                }
                catch (Exception ex)
                {
                    // TBD: Add you error handling here
                    throw;
                }
            }

            return B2CGraphClient.AccessToken.AccessToken;
        }

        /// <summary>
        /// Generate temporary password
        /// </summary>
        private static string GeneratePassword()
        {
            const string A = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string a = "abcdefghijklmnopqrstuvwxyz";
            const string num = "1234567890";
            const string spe = "!@#$!&";

            string rv = GenerateLetters(4, A) + GenerateLetters(4, a) + GenerateLetters(4, num) + GenerateLetters(1, spe);
            return rv;
        }

        /// <summary>
        /// Generate random letters from string of letters
        /// </summary>
        private static string GenerateLetters(int length, string baseString)
        {
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(baseString[rnd.Next(baseString.Length)]);
            }
            return res.ToString();
        }
    }
}
