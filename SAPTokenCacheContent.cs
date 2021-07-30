using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Simple.OData.Client;

namespace AzureSAPODataReader
{
    public class SAPTokenCacheContent
    {
        private readonly IConfiguration _Configuration;
        public IList<string> cookies{get;set;} = new List<string>();

        public SAPTokenCacheContent(IConfiguration configuration, string userIdentifier, string url)
        {
            _Configuration = configuration;
            this.userIdentifier = userIdentifier;
            this.url = url;
        }
        public string accessToken { get; set; }
        public string csrfToken { get; set; } = "";
        public string userIdentifier { get;  }
        public string url { get;  }

        public ODataClientSettings getODataClientSettings()
        {
            var oDataClientSettings = new ODataClientSettings(new Uri(url));
            //ignore cookie container so we can set SAP cookies ourselves
            //https://github.com/simple-odata-client/Simple.OData.Client/issues/37
            oDataClientSettings.OnApplyClientHandler = handler => handler.UseCookies = false;
            oDataClientSettings.OnTrace = (x, y) => Console.WriteLine("TRACE---->" + string.Format(x, y));
            oDataClientSettings.BeforeRequest += delegate (HttpRequestMessage message)
            {
                //message.Headers.Add("Authorization", Configuration.GetValue<string>("SAPODataAPI:BasicAuth"));
                message.Headers.Add("Authorization", "Bearer " + accessToken);
                if(message.Method.Equals(HttpMethod.Get)){
                    message.Headers.Add("X-CSRF-Token", "Fetch");
                }else{
                    message.Headers.Add("X-CSRF-Token", csrfToken);
                    foreach (string cookie in cookies){
                        message.Headers.Add("Cookie", cookie);
                    }
                }
                message.Headers.Add("Ocp-Apim-Subscription-Key", _Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Subscription-Key"));
                message.Headers.Add("Ocp-Apim-Trace", _Configuration.GetValue<string>("SAPODataAPI:Ocp-Apim-Trace"));
            };

            oDataClientSettings.AfterResponse += delegate (HttpResponseMessage message)
            {
                //if (message.StatusCode == HttpStatusCode.OK){
                    if(message.Headers.Contains("X-CSRF-Token"))
                    {
                        csrfToken = message.Headers.GetValues("X-CSRF-Token").FirstOrDefault();
                        cookies = message.Headers.GetValues("Set-Cookie").ToList();
                    }
                //}
            };

            return oDataClientSettings;
        }
    }

}