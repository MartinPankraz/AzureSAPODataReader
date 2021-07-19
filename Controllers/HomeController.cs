﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureODataReader.Models;
using Simple.OData.Client;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace AzureODataReader.Controllers
{
    //[Authorize]
    public class HomeController : Controller
    {

        public HomeController(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        private string myCSRFToken = "";
        public async Task<IActionResult> Index()
        {   
            var accessToken = "";//HttpContext.GetTokenAsync("access_token").Result;
            var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("Modules:AzureAPIM:BaseURL"), accessToken));

            /*var x = ODataDynamic.Expression;
            IEnumerable<dynamic> _values = await _client
                .For(x.Products)
                .Top(10)
                .FindEntriesAsync();*/
            var products = await client
                .For<ProductViewModel>("Products")
                .Top(10)
                .FindEntriesAsync();

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Details(string id)
        {
            return View();
        }

        public IActionResult Delete(string id)
        {
            if (id == null)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            return View();
        }

        public async Task<IActionResult> Edit(string id)
        {
            var accessToken = "";//HttpContext.GetTokenAsync("access_token").Result;
            var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("Modules:AzureAPIM:BaseURL"), accessToken));
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
                var accessToken = "";//HttpContext.GetTokenAsync("access_token").Result;
                var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("Modules:AzureAPIM:BaseURL"), accessToken));
                ProductViewModel product = await client
                    .For<ProductViewModel>("Products")
                    .Key(model.Id)
                    .Set(new { Price = model.Price })
                    .UpdateEntryAsync();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        private ODataClientSettings SetODataToken(string url, string accessToken)
        {
            var oDataClientSettings = new ODataClientSettings(new Uri(url));
            //oDataClientSettings.OnApplyClientHandler = handler => handler.PreAuthenticate = false;
            oDataClientSettings.OnTrace = (x, y) => Console.WriteLine("TRACE---->"+string.Format(x, y));
            oDataClientSettings.BeforeRequest += delegate (HttpRequestMessage message)
            {
                message.Headers.Add("Authorization", Configuration.GetValue<string>("Modules:AzureAPIM:BasicAuth"));
                if(this.myCSRFToken == ""){
                    message.Headers.Add("X-CSRF-Token", "Fetch");
                }else{
                    message.Headers.Add("X-CSRF-Token", this.myCSRFToken);
                }
                //message.Headers.Add("Authorization", "Bearer " + accessToken);
                message.Headers.Add("Ocp-Apim-Subscription-Key", Configuration.GetValue<string>("Modules:AzureAPIM:Ocp-Apim-Subscription-Key"));
                message.Headers.Add("Ocp-Apim-Trace", Configuration.GetValue<string>("Modules:AzureAPIM:Ocp-Apim-Trace"));
            };

            oDataClientSettings.AfterResponse += delegate (HttpResponseMessage message)
            {
                if(message.Headers.Contains("x-csrf-token"))
                {
                    this.myCSRFToken = message.Headers.GetValues("x-csrf-token").First();
                }
            };
 
            return oDataClientSettings;
        }   
    }
}
