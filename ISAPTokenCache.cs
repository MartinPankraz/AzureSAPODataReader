using System;
using System.Threading.Tasks;

namespace AzureSAPODataReader
{

    public interface ISAPTokenCache
    {
        Task<SAPTokenCacheContent> GetSAPTokenCacheContentAsync(string uniqueUserIdentifier, string url);
    }

}