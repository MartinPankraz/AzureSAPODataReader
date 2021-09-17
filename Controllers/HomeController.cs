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
using System.Text;
using Microsoft.AspNetCore.Authentication;

namespace AzureSAPODataReader.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ITokenAcquisition TokenAcquisition;
        private string baseAPIUrl;
        public HomeController(IConfiguration configuration, ITokenAcquisition tokenAcquisition, ISAPTokenCache tokenCache = null)
        {
            _Configuration = configuration;
            //token cache will be null if APIM policy used for SAP principal propagation instead of client-side caching.
            TokenCache = tokenCache;
            baseAPIUrl = configuration.GetValue<string>("AzureAd:ApiBaseAddress");
            TokenAcquisition = tokenAcquisition;
        }

        public IConfiguration _Configuration { get; }
        public ISAPTokenCache TokenCache { get; }

        public async Task<IActionResult> Index()
        {
            // Acquire the access token.
            string[] scopes = new string[] { _Configuration.GetValue<string>("AzureAd:ScopeForAccessToken") };
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
            ODataClient client = null;
            if(TokenCache != null)
            {
                //If ISAPTokenCache is injected, use it to get the token cache
                SAPTokenCacheContent content = await TokenCache.GetSAPTokenCacheContentAsync(accessToken, baseAPIUrl);
                client = new ODataClient(content.getODataClientSettingsAsync());//SetODataToken(Configuration.GetValue<string>("AzureAd:ApiBaseAddress"), content.accessToken, true));
            }else
            {
                //assume this is done server-side by a proxy like Azure APIM (using policy)
                client = new ODataClient(getODataClientSettings(accessToken));
            }
            
            return client;
        }

        private ODataClientSettings getODataClientSettings(string accessToken)
        {
            var myClientOverride = new HttpClient() { BaseAddress = new Uri(baseAPIUrl) };
            myClientOverride.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            //myClientOverride.DefaultRequestHeaders.Add("Authorization", Configuration.GetValue<string>("AzureAd:BasicAuth"));
            //myClientOverride.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _Configuration.GetValue<string>("AzureAd:Ocp-Apim-Subscription-Key"));
            myClientOverride.DefaultRequestHeaders.Add("Ocp-Apim-Trace", _Configuration.GetValue<string>("AzureAd:Ocp-Apim-Trace"));

            var oDataClientSettings = new ODataClientSettings(myClientOverride);
            oDataClientSettings.OnTrace = (x, y) => Console.WriteLine("TRACE---->" + string.Format(x, y));

            return oDataClientSettings;
        }
        //plain http implementation for initial test
        private async Task<HttpClient> getHttpClientForUsername(string[] scopes)
        {
            string accessToken = await TokenAcquisition.GetAccessTokenForUserAsync(scopes);
            SAPTokenCacheContent content = await TokenCache.GetSAPTokenCacheContentAsync(accessToken, baseAPIUrl);

            var handler = new HttpClientHandler(){
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {content.accessToken}");
            //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _Configuration.GetValue<string>("AzureAd:Ocp-Apim-Subscription-Key"));
            client.BaseAddress = new Uri(baseAPIUrl);
            return client;
        }

        public async Task<IActionResult> Edit(string id)
        {
            // Acquire the access token.
            string[] scopes = new string[] { _Configuration.GetValue<string>("AzureAd:ScopeForAccessToken") };
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
                string[] scopes = new string[] { _Configuration.GetValue<string>("AzureAd:ScopeForAccessToken") };
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
