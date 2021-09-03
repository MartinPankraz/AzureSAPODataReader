using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AzureODataReader.Models;
using Simple.OData.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;

namespace AzureSAPODataReader
{
    public class MemorySAPTokenCache : ISAPTokenCache
    {
        public MemorySAPTokenCache(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _Configuration = configuration;
            this.httpClientFactory = httpClientFactory;
        }
        private Dictionary<string, SAPTokenCacheContent> _cache = new Dictionary<string, SAPTokenCacheContent>();
        private readonly IHttpClientFactory httpClientFactory;

        public IConfiguration _Configuration { get; }

        public async Task<SAPTokenCacheContent> GetSAPTokenCacheContentAsync(string AADTokenContainingUniqueUserIdentifier, string SAPUrl)
        {
            //get subject from bearer token
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(AADTokenContainingUniqueUserIdentifier);
            var user = token.Claims.FirstOrDefault(c => c.Type == "upn").Value;
            var identifier = user + "|" + SAPUrl;
            if (_cache.ContainsKey(identifier))
            {
                if(!_cache[identifier].IsExpired()){
                    return _cache[identifier];
                }else{
                    _cache.Remove(identifier);
                    return await getSAPToken(AADTokenContainingUniqueUserIdentifier, SAPUrl, user, identifier);
                }
            }else
            {
                return await getSAPToken(AADTokenContainingUniqueUserIdentifier, SAPUrl, user, identifier);
            }
        }

        private async Task<SAPTokenCacheContent> getSAPToken(string AADTokenContainingUniqueUserIdentifier, string SAPUrl, string user, string identifier)
        {
            var myTokenCache = new SAPTokenCacheContent(_Configuration, user, SAPUrl);
            var assertion = await getSAMLFromBearerToken(AADTokenContainingUniqueUserIdentifier);
            myTokenCache.accessToken = assertion.access_token;
            myTokenCache.expiresAt = DateTime.UtcNow.AddSeconds(double.Parse(assertion.expires_in));//_Configuration.GetValue<int>("SAPOAuthAPI:SAPTokenCacheExpirationInSeconds"));
            _cache.Add(identifier, myTokenCache);
            return myTokenCache;
        }


        private async Task<SAML2BearerResponseModel> getSAMLFromBearerToken(string accessToken)
        {
            /*var handler = new HttpClientHandler(){
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };*/
            var client = httpClientFactory.CreateClient();
            var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"));
                nvc.Add(new KeyValuePair<string, string>("assertion", accessToken));
                nvc.Add(new KeyValuePair<string, string>("client_id", _Configuration.GetValue<string>("AzureAd:ClientId")));
                nvc.Add(new KeyValuePair<string, string>("client_secret", _Configuration.GetValue<string>("AzureAd:ClientSecret")));
                nvc.Add(new KeyValuePair<string, string>("resource", _Configuration.GetValue<string>("SAPODataAPI:SAPSysResourceID")));
                nvc.Add(new KeyValuePair<string, string>("requested_token_use", "on_behalf_of"));
                nvc.Add(new KeyValuePair<string, string>("requested_token_type", "urn:ietf:params:oauth:token-type:saml2"));
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://login.microsoftonline.com/" + _Configuration.GetValue<string>("AzureAd:TenantId") + "/oauth2/token"),
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
                var OAuthServiceResponse = await JsonSerializer.DeserializeAsync<OAuthResponseModel>(contentStream, new JsonSerializerOptions { IgnoreNullValues = true, PropertyNameCaseInsensitive = true });
                var finalSAPBearerResponse = await getSAML2BearerToken(OAuthServiceResponse.access_token);
                return finalSAPBearerResponse;                
            }
            else
            {
                throw new Exception("Invalid response from SAP OAuth API.");
            }
        }

        private async Task<SAML2BearerResponseModel> getSAML2BearerToken(string samlToken)
        {
            /*var handler = new HttpClientHandler(){
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };*/
            var client = httpClientFactory.CreateClient();
            var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:saml2-bearer"));
                nvc.Add(new KeyValuePair<string, string>("assertion", samlToken));
                nvc.Add(new KeyValuePair<string, string>("client_id", _Configuration.GetValue<string>("SAPOAuthAPI:client_id")));
                nvc.Add(new KeyValuePair<string, string>("scope", _Configuration.GetValue<string>("SAPOAuthAPI:scope")));
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_Configuration.GetValue<string>("SAPOAuthAPI:ApiBaseAddress")),
                Headers = { 
                    { HttpRequestHeader.Authorization.ToString(), _Configuration.GetValue<string>("SAPOAuthAPI:BasicAuth") },
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { "Ocp-Apim-Subscription-Key", _Configuration.GetValue<string>("SAPOAuthAPI:Ocp-Apim-Subscription-Key") },
                    { "Ocp-Apim-Trace", _Configuration.GetValue<string>("SAPOAuthAPI:Ocp-Apim-Trace") }
                },
                Content = new FormUrlEncodedContent(nvc)
            };

            var httpResponse = client.SendAsync(httpRequestMessage).Result;
            httpResponse.EnsureSuccessStatusCode(); // throws if not 200-299

            if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == "application/json")
            {
                var contentStream = await httpResponse.Content.ReadAsStreamAsync();
                var OAuthServiceResponse = await JsonSerializer.DeserializeAsync<SAML2BearerResponseModel>(contentStream, new JsonSerializerOptions { IgnoreNullValues = true, PropertyNameCaseInsensitive = true });
                return OAuthServiceResponse;
            }
            else
            {
                throw new Exception("HTTP Response was invalid and cannot be deserialised.");
            }
        }
    }
}