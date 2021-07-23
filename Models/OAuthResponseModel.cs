using System;

namespace AzureODataReader.Models
{
    public class OAuthResponseModel
    {
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string ext_expires_in { get; set; }
        public string expires_on { get; set; }
        public string resource { get; set; }
        public string access_token { get; set; }
        public string issued_token_type { get; set; }
        public string refresh_token { get; set; }
    
    }
}
