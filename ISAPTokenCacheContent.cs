using Simple.OData.Client;

namespace AzureSAPODataReader
{
    public interface ISAPTokenCacheContent
    {
        string accessToken { get; set; }
        string userIdentifier { get; }
        string url { get; }

        ODataClientSettings getODataClientSettingsAsync();
        bool IsExpired();
    }

}