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
            string bearer = await getSAMLFromBearerToken(accessToken);

            var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress"), bearer));

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
            string bearer = await getSAMLFromBearerToken(accessToken);

            var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress"), bearer));
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
                string bearer = await getSAMLFromBearerToken(accessToken);

                var client = new ODataClient(SetODataToken(Configuration.GetValue<string>("SAPODataAPI:ApiBaseAddress"), bearer));
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

        private async Task<string> getSAMLFromBearerToken(string accessToken)
        {
            var client = new HttpClient();
            var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"));
                nvc.Add(new KeyValuePair<string, string>("assertion", accessToken));
                nvc.Add(new KeyValuePair<string, string>("client_id", Configuration.GetValue<string>("AzureAd:ClientId")));
                nvc.Add(new KeyValuePair<string, string>("client_secret", Configuration.GetValue<string>("AzureAd:ClientSecret")));
                nvc.Add(new KeyValuePair<string, string>("resource", Configuration.GetValue<string>("SAPODataAPI:SAPSysResourceID")));
                nvc.Add(new KeyValuePair<string, string>("requested_token_use", "on_behalf_of"));
                nvc.Add(new KeyValuePair<string, string>("requested_token_type", "urn:ietf:params:oauth:token-type:saml2"));
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://login.microsoftonline.com/" + Configuration.GetValue<string>("AzureAd:TenantId") + "/oauth2/token"),
                Headers = { 
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                    { HttpRequestHeader.Accept.ToString(), "application/json" }
                },
                Content = new FormUrlEncodedContent(nvc)
            };

            var httpResponse = client.SendAsync(httpRequestMessage).Result;
            httpResponse.EnsureSuccessStatusCode(); // throws if not 200-299

            if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == "application/json")
            {
                var contentStream = await httpResponse.Content.ReadAsStreamAsync();

                try
                {
                    var OAuthServiceResponse = await JsonSerializer.DeserializeAsync<OAuthResponseModel>(contentStream, new JsonSerializerOptions { IgnoreNullValues = true, PropertyNameCaseInsensitive = true });
                    var finalSAPBearerToken = await getSAML2BearerToken(OAuthServiceResponse.access_token);
                    return finalSAPBearerToken;
                }
                catch (JsonException) // Invalid JSON
                {
                    Console.WriteLine("Invalid JSON.");
                }                
            }
            else
            {
                Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
            }

            return "";
        }

        private async Task<string> getSAML2BearerToken(string samlToken)
        {
            var client = new HttpClient();
            var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:saml2-bearer"));
                nvc.Add(new KeyValuePair<string, string>("assertion", samlToken));
                nvc.Add(new KeyValuePair<string, string>("client_id", Configuration.GetValue<string>("SAPOAuthAPI:client_id")));
                nvc.Add(new KeyValuePair<string, string>("scope", Configuration.GetValue<string>("SAPOAuthAPI:scope")));
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(Configuration.GetValue<string>("SAPOAuthAPI:ApiBaseAddress")),
                Headers = { 
                    { HttpRequestHeader.Authorization.ToString(), Configuration.GetValue<string>("SAPOAuthAPI:BasicAuth") },
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { "Ocp-Apim-Subscription-Key", Configuration.GetValue<string>("SAPOAuthAPI:Ocp-Apim-Subscription-Key") },
                    { "Ocp-Apim-Trace", Configuration.GetValue<string>("SAPOAuthAPI:Ocp-Apim-Trace") }
                },
                Content = new FormUrlEncodedContent(nvc)
            };

            var httpResponse = client.SendAsync(httpRequestMessage).Result;
            httpResponse.EnsureSuccessStatusCode(); // throws if not 200-299

            if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == "application/json")
            {
                var contentStream = await httpResponse.Content.ReadAsStreamAsync();

                try
                {
                    var OAuthServiceResponse = await JsonSerializer.DeserializeAsync<SAML2BearerResponseModel>(contentStream, new JsonSerializerOptions { IgnoreNullValues = true, PropertyNameCaseInsensitive = true });
                    return OAuthServiceResponse.access_token;
                }
                catch (JsonException) // Invalid JSON
                {
                    Console.WriteLine("Invalid JSON.");
                }                
            }
            else
            {
                Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
            }

            return "";
        }

        private ODataClientSettings SetODataToken(string url, string accessToken)
        {
            var oDataClientSettings = new ODataClientSettings(new Uri(url));
            //oDataClientSettings.OnApplyClientHandler = handler => handler.PreAuthenticate = false;
            oDataClientSettings.OnTrace = (x, y) => Console.WriteLine("TRACE---->" + string.Format(x, y));
            oDataClientSettings.BeforeRequest += delegate (HttpRequestMessage message)
            {
                //message.Headers.Add("Authorization", Configuration.GetValue<string>("SAPODataAPI:BasicAuth"));
                message.Headers.Add("Authorization", "Bearer " + accessToken);
                message.Headers.Add("Ocp-Apim-Subscription-Key", Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Subscription-Key"));
                message.Headers.Add("Ocp-Apim-Trace", Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Trace"));
            };

            return oDataClientSettings;
        }
    }
}
