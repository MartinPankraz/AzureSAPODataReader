using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureSAPODataReader.Models;
using Microsoft.Extensions.Configuration;
using Simple.OData.Client;
using AzureODataReader.Models;
using System.Net.Http;
using Microsoft.Identity.Web;
using System.Net;
using System.Text.Json;

namespace AzureSAPODataReader.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ITokenAcquisition TokenAcquisition;
        private string baseAPIUrl;
        public HomeController(IConfiguration configuration, ISAPTokenCache tokenCache, ITokenAcquisition tokenAcquisition)
        {
            _Configuration = configuration;
            TokenCache = tokenCache;
            baseAPIUrl = configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress");
            TokenAcquisition = tokenAcquisition;
        }

        public IConfiguration _Configuration { get; }
        public ISAPTokenCache TokenCache { get; }

        public async Task<IActionResult> Index()
        {
            // Acquire the access token.
            string[] scopes = new string[] { _Configuration.GetValue<string>("SAPODataAPI:ScopeForAccessToken") };
            ODataClient client = await getODataClientForUsername(scopes);

            var products = await client
                .For<ProductViewModel>("Products")
                .Top(10)
                .FindEntriesAsync();

            return View(products);
        }

        private async Task<ODataClient> getODataClientForUsername(string[] scopes)
        {
            string accessToken = await TokenAcquisition.GetAccessTokenForUserAsync(scopes);
            SAPTokenCacheContent content = await TokenCache.GetSAPTokenCacheContentAsync(accessToken, baseAPIUrl);

            var client = new ODataClient(content.getODataClientSettings());//SetODataToken(Configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress"), content.accessToken, true));
            return client;
        }

        public async Task<IActionResult> Edit(string id)
        {
            // Acquire the access token.
            string[] scopes = new string[] { _Configuration.GetValue<string>("SAPODataAPI:ScopeForAccessToken") };
            ODataClient client = await getODataClientForUsername(scopes);

            ProductViewModel product = await client
                .For<ProductViewModel>("Products")
                .Key(id)
                .FindEntryAsync();

            ViewBag.ProductId = id;

            return View(product);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Acquire the access token.
                string[] scopes = new string[] { _Configuration.GetValue<string>("SAPODataAPI:ScopeForAccessToken") };
                ODataClient client = await getODataClientForUsername(scopes);

                ProductViewModel product = await client
                    .For<ProductViewModel>("Products")
                    .Key(model.Id)
                    .Set(new { Price = model.Price })
                    .UpdateEntryAsync();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        
    }
}
