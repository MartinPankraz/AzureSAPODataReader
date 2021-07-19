using System;
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

namespace AzureODataReader.Controllers
{
    public class HomeController : Controller
    {

        public HomeController(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public async Task<IActionResult> Index()
        {   
            var accessToken = "";//HttpContext.GetTokenAsync("access_token").Result;
            var _client = new ODataClient(SetODataToken(Configuration.GetValue<string>("Modules:AzureAPIM:BaseURL"), accessToken));

            var x = ODataDynamic.Expression;
            IEnumerable<dynamic> values = await _client
                .For(x.Products)
                .Top(10)
                .FindEntriesAsync();

            return View(values);
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

        private ODataClientSettings SetODataToken(string url, string accessToken)
    {
            var oDataClientSettings = new ODataClientSettings(new Uri(url));
            //oDataClientSettings.OnApplyClientHandler = handler => handler.PreAuthenticate = false;
            //oDataClientSettings.OnTrace = (x, y) => Console.WriteLine("TRACE---->"+string.Format(x, y));
            oDataClientSettings.BeforeRequest += delegate (HttpRequestMessage message)
            {
                message.Headers.Add("Authorization", Configuration.GetValue<string>("Modules:AzureAPIM:BasicAuth"));
                //message.Headers.Add("Authorization", "Bearer " + accessToken);
                message.Headers.Add("Ocp-Apim-Subscription-Key", Configuration.GetValue<string>("Modules:AzureAPIM:Ocp-Apim-Subscription-Key"));
                message.Headers.Add("Ocp-Apim-Trace", Configuration.GetValue<string>("Modules:AzureAPIM:Ocp-Apim-Trace"));
            };
 
        return oDataClientSettings;
    }
    }
}
