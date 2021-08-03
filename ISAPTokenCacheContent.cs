using System;
using Simple.OData.Client;

namespace AzureSAPODataReader
{
    public interface ISAPTokenCacheContent
    {
        string accessToken { get; set; }
        string userIdentifier { get; }
        string url { get; }
        DateTime expiresAt { get; set; }

        ODataClientSettings getODataClientSettingsAsync();
        bool IsExpired();
    }

}