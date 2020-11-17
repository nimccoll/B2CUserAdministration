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
using AADB2C.UserAdmin.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AADB2C.UserAdmin.Controllers
{
    public class HomeController : BaseController
    {
        private readonly string _tenant = ConfigurationManager.AppSettings["b2c:Tenant"];
        private readonly string _clientId = ConfigurationManager.AppSettings["b2c:ClientId"];
        private readonly string _clientSecret = ConfigurationManager.AppSettings["b2c:ClientSecret"];
        private B2CGraphClient _b2CGraphClient;
        private SearchClient _searchClient;

        public HomeController()
        {
            _b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            _searchClient = new SearchClient(new Uri(ConfigurationManager.AppSettings["AZURE_SEARCH_URI"]), ConfigurationManager.AppSettings["AZURE_SEARCH_INDEX"], new AzureKeyCredential(ConfigurationManager.AppSettings["AZURE_SEARCH_KEY"]));
        }

        [Authorize]
        public ActionResult Index()
        {
            if (CurrentUser.IsInRole("Admin"))
            {
                ViewBag.Message = "Use this application to create and manage users in your organization.";
                ViewBag.IsAdmin = true;
            }
            else
            {
                ViewBag.Message = "You do not have permission to manage users in your organization.";
                ViewBag.IsAdmin = false;
            }
            ViewBag.CurrentOrganization = CurrentUser.Organization;

            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> List(int startRow = 1, int numberOfRows = 5)
        {
            if (!CurrentUser.IsInRole("Admin")) return RedirectToAction("Index");

            long? numberOfDocuments = 0;
            List<Document> documents = new List<Document>();

            if (!string.IsNullOrEmpty(CurrentUser.Organization))
            {
                // Create a search client and set the search options
                SearchOptions searchOptions = new SearchOptions();
                searchOptions.IncludeTotalCount = true;
                searchOptions.SearchFields.Add("organization");
                searchOptions.Skip = startRow - 1;
                searchOptions.Size = numberOfRows;
                searchOptions.OrderBy.Add("lastName");
                searchOptions.OrderBy.Add("firstName");

                // Retrieve the matching documents
                SearchResults<Document> searchResults = await _searchClient.SearchAsync<Document>(CurrentUser.Organization, searchOptions);
                numberOfDocuments = searchResults.TotalCount;
                AsyncPageable<SearchResult<Document>> results = searchResults.GetResultsAsync();

                await foreach (SearchResult<Document> result in results)
                {
                    // Decode the issuer ID of the user
                    result.Document.issuerId = Encoding.UTF8.GetString(Convert.FromBase64String(result.Document.issuerId));
                    documents.Add(result.Document);
                }
            }

            // Configure paging controls
            if (startRow > 1)
            {
                ViewBag.PreviousClass = "previous";
                ViewBag.PreviousRow = startRow - numberOfRows;
            }
            else
            {
                ViewBag.PreviousClass = "previous disabled";
                ViewBag.PreviousRow = startRow;
            }
            if (startRow + numberOfRows > numberOfDocuments.Value)
            {
                ViewBag.NextClass = "next disabled";
                ViewBag.NextRow = numberOfDocuments;
            }
            else
            {
                ViewBag.NextClass = "next";
                ViewBag.NextRow = startRow + numberOfRows;
            }
            ViewBag.NumberOfRows = numberOfRows;

            return View(documents);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Create()
        {
            if (!CurrentUser.IsInRole("Admin")) return RedirectToAction("Index");

            AccountModel model = new AccountModel() { accountType = "Local Account", extension_Organization = CurrentUser.Organization, extension_UserRole = "User" };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Create(AccountModel model)
        {
            if (!CurrentUser.IsInRole("Admin")) return RedirectToAction("Index");

            B2CGraphClient b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            GraphAccountModel newUser = await b2CGraphClient.CreateAccount("emailAddress",
                model.signInName,
                model.issuer,
                model.issuerUserId,
                model.email,
                model.password,
                model.displayName,
                model.firstName,
                model.lastName,
                model.extension_Organization,
                model.extension_UserRole,
                true);
            if (newUser != null)
            {
                // Update the Azure Search Index
                Document document = new Document()
                {
                    id = newUser.objectId,
                    signInName = model.signInName,
                    issuer = model.issuer,
                    issuerId = model.issuerUserId,
                    email = model.email,
                    displayName = model.displayName,
                    firstName = model.firstName,
                    lastName = model.lastName,
                    organization = model.extension_Organization,
                    userRole = model.extension_UserRole
                };
                List<Document> documents = new List<Document>() { document };
                IndexDocumentsResult indexResults = await _searchClient.MergeOrUploadDocumentsAsync(documents);
                ViewBag.Message = "User created successfully!";
            }
            else
            {
                ViewBag.Message = "User creation failed!";
            }
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Edit(string objectId)
        {
            if (!CurrentUser.IsInRole("Admin")) return RedirectToAction("Index");

            AccountModel model;
            B2CGraphClient b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            GraphAccountModel user = await b2CGraphClient.GetUserByObjectId(objectId);

            if (user != null)
            {
                model = new AccountModel()
                {
                    objectId = user.objectId,
                    displayName = user.displayName,
                    firstName = user.givenName,
                    lastName = user.surname,
                    extension_Organization = user.extension_Organization,
                    extension_UserRole = user.extension_UserRole
                };
                if (user.signInNames != null && user.signInNames.Count > 0) model.signInName = user.signInNames[0].value;
                if (user.userIdentities != null && user.userIdentities.Count > 0)
                {
                    model.issuer = user.userIdentities[0].issuer;
                    model.issuerUserId = Encoding.UTF8.GetString(Convert.FromBase64String(user.userIdentities[0].issuerUserId));
                }
                if (string.IsNullOrEmpty(user.creationType)) model.accountType = "Federated Active Directory";
                else model.accountType = "Local Account";
            }
            else
            {
                return RedirectToAction("Find");
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Edit(AccountModel model)
        {
            if (!CurrentUser.IsInRole("Admin")) return RedirectToAction("Index");

            B2CGraphClient b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            await b2CGraphClient.UpdateUser(model.objectId, model.displayName, model.firstName, model.lastName, model.extension_Organization, model.extension_UserRole);

            // Update the Azure Search Index
            Document document = new Document()
            {
                id = model.objectId,
                signInName = model.signInName,
                issuer = model.issuer,
                issuerId = Convert.ToBase64String(Encoding.UTF8.GetBytes(model.issuerUserId)),
                email = model.email,
                displayName = model.displayName,
                firstName = model.firstName,
                lastName = model.lastName,
                organization = model.extension_Organization,
                userRole = model.extension_UserRole
            };
            List<Document> documents = new List<Document>() { document };
            IndexDocumentsResult indexResults = await _searchClient.MergeOrUploadDocumentsAsync(documents);

            ViewBag.Message = "User updated successfully!";
            return View(model);
        }
    }
}