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
using AADB2C.GraphService;
using AADB2C.UserMigration.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace AADB2C.UserMigration
{
    class Program
    {
        public static string Tenant = ConfigurationManager.AppSettings["b2c:Tenant"];
        public static string ClientId = ConfigurationManager.AppSettings["b2c:ClientId"];
        public static string ClientSecret = ConfigurationManager.AppSettings["b2c:ClientSecret"];
        public static string MigrationFile = ConfigurationManager.AppSettings["MigrationFile"];

        static void Main(string[] args)
        {
            try
            {
                MigrateUsersWithRandomPasswordAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
            }
            finally
            {
                Console.ResetColor();
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Migrate users with random password
        /// </summary>
        /// <returns></returns>
        static async Task MigrateUsersWithRandomPasswordAsync()
        {
            string appDirecotyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string dataFilePath = Path.Combine(appDirecotyPath, Program.MigrationFile);

            // Check file existence 
            if (!File.Exists(dataFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File '{dataFilePath}' not found");
                Console.ResetColor();
                return;
            }

            // Read the data file and convert to object
            LocalAccountsModel users = LocalAccountsModel.Parse(File.ReadAllText(dataFilePath));

            // Create B2C graph client object 
            B2CGraphClient b2CGraphClient = new B2CGraphClient(Program.Tenant, Program.ClientId, Program.ClientSecret);

            // Create Search client object
            SearchClient searchClient = new SearchClient(new Uri(ConfigurationManager.AppSettings["AZURE_SEARCH_URI"]), ConfigurationManager.AppSettings["AZURE_SEARCH_INDEX"], new AzureKeyCredential(ConfigurationManager.AppSettings["AZURE_SEARCH_KEY"]));
            
            int successes = 0;
            int fails = 0;

            foreach (var item in users.Users)
            {
                GraphAccountModel newUser = await b2CGraphClient.CreateAccount(users.userType,
                    item.signInName,
                    item.issuer,
                    item.issuerUserId,
                    item.email,
                    item.password,
                    item.displayName,
                    item.firstName,
                    item.lastName,
                    item.extension_Organization,
                    item.extension_UserRole,
                    true);

                if (newUser != null)
                {
                    // Update the Azure Search Index
                    string signInName = string.Empty;
                    string issuer = string.Empty;
                    string issuerId = string.Empty;
                    string email = string.Empty;
                    if (newUser.signInNames != null && newUser.signInNames.Count > 0) signInName = newUser.signInNames[0].value;
                    if (newUser.userIdentities != null && newUser.userIdentities.Count > 0)
                    {
                        issuer = newUser.userIdentities[0].issuer;
                        issuerId = newUser.userIdentities[0].issuerUserId;
                    }
                    if (newUser.otherMails != null && newUser.otherMails.Count > 0) email = newUser.otherMails[0];
                    Document document = new Document()
                    {
                        id = newUser.objectId,
                        signInName = signInName,
                        issuer = issuer,
                        issuerId = issuerId,
                        email = email,
                        displayName = newUser.displayName,
                        firstName = newUser.givenName,
                        lastName = newUser.surname,
                        organization = newUser.extension_Organization,
                        userRole = newUser.extension_UserRole
                    };
                    List<Document> documents = new List<Document>() { document };
                    IndexDocumentsResult indexResults = await searchClient.MergeOrUploadDocumentsAsync(documents);
                    successes += 1;
                }
                else
                    fails += 1;

            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\r\nUsers migration report:\r\n\tSuccesses: {successes}\r\n\tFails: {fails} ");
            Console.ResetColor();
        }
    }
}
