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

namespace AzureSAPODataReader.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        readonly ITokenAcquisition tokenAcquisition;
        public HomeController(IConfiguration configuration, ITokenAcquisition tokenAcquisition)
        {
            Configuration = configuration;
            this.tokenAcquisition = tokenAcquisition;
        }

        public IConfiguration Configuration { get; }
        public async Task<IActionResult> Index()
        {
            // Acquire the access token.
            string[] scopes = new string[]{Configuration.GetValue<string>("SAPODataAPI:ScopeForAccessToken")};
            string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress"), accessToken));

            var products = await client
                .For<ProductViewModel>("Products")
                .Top(10)
                .FindEntriesAsync();

            return View(products);
        }

        public async Task<IActionResult> Edit(string id)
        {
            // Acquire the access token.
            string[] scopes = new string[]{Configuration.GetValue<string>("SAPODataAPI:ScopeForAccessToken")};
            string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress"), accessToken));
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
                string[] scopes = new string[]{Configuration.GetValue<string>("SAPODataAPI:ScopeForAccessToken")};
                string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress"), accessToken));
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

        private ODataClientSettings SetODataToken(string url, string accessToken)
        {
            var oDataClientSettings = new ODataClientSettings(new Uri(url));
            //oDataClientSettings.OnApplyClientHandler = handler => handler.PreAuthenticate = false;
            oDataClientSettings.OnTrace = (x, y) => Console.WriteLine("TRACE---->" + string.Format(x, y));
            oDataClientSettings.BeforeRequest += delegate (HttpRequestMessage message)
            {
                message.Headers.Add("Authorization", Configuration.GetValue<string>("SAPODataAPI:BasicAuth"));
                //message.Headers.Add("Authorization", "Bearer " + accessToken);
                message.Headers.Add("Ocp-Apim-Subscription-Key", Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Subscription-Key"));
                message.Headers.Add("Ocp-Apim-Trace", Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Trace"));
            };

            return oDataClientSettings;
        }
    }
}
