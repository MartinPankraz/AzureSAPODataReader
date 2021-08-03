using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Simple.OData.Client;

namespace AzureSAPODataReader
{

    public class SAPTokenCacheContent : ISAPTokenCacheContent
    {
        private readonly IConfiguration _Configuration;

        public SAPTokenCacheContent(IConfiguration configuration, string userIdentifier, string url)
        {
            _Configuration = configuration;
            this.userIdentifier = userIdentifier;
            this.url = url;
        }
        public string accessToken { get; set; }
        public string userIdentifier { get; }
        public string url { get; }
        public DateTime expiresAt { get; set; }

        public bool IsExpired()
        {

            //var SAPHandler = new JwtSecurityTokenHandler();
            //Todo: check SAP token type to read it
            //var SAPtoken = SAPHandler.ReadJwtToken(accessToken);
            return expiresAt < DateTime.UtcNow;//SAPtoken.ValidTo < now;
        }

        public ODataClientSettings getODataClientSettingsAsync()
        {
            var myClientOverride = new HttpClient() { BaseAddress = new Uri(url) };
            myClientOverride.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            //myClientOverride.DefaultRequestHeaders.Add("Authorization", Configuration.GetValue<string>("SAPODataAPI:BasicAuth"));
            myClientOverride.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Subscription-Key"));
            myClientOverride.DefaultRequestHeaders.Add("Ocp-Apim-Trace", _Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Trace"));

            var oDataClientSettings = new ODataClientSettings(myClientOverride);
            //ignore cookie container so we can set SAP cookies ourselves
            //https://github.com/simple-odata-client/Simple.OData.Client/issues/37
            //oDataClientSettings.OnApplyClientHandler = handler => handler.UseCookies = false;
            /*oDataClientSettings.OnApplyClientHandler = handler =>{
                //Remove this line for production
                handler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
            };*/
            oDataClientSettings.OnTrace = (x, y) => Console.WriteLine("TRACE---->" + string.Format(x, y));
            oDataClientSettings.BeforeRequest += delegate (HttpRequestMessage message)
            {
                //preflight x-crf-token fetch, because odata client closes http connection each time it makes a request
                if (message.Method != HttpMethod.Get)
                {
                    HttpClient csrfClient = oDataClientSettings.HttpClient;
                    HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Head, url + "/");
                    msg.Headers.Add("x-csrf-token", "Fetch");

                    HttpResponseMessage responseMessage = csrfClient.SendAsync(msg).Result;

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        message.Headers.Add("x-csrf-token", responseMessage.Headers.GetValues("x-csrf-token").FirstOrDefault());
                    }
                }
            };

            return oDataClientSettings;
        }

    }

}