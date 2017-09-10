using System.Collections.Generic;

namespace wamTest
{
    public interface ISettings
    {
        IEnumerable<string> SupportedVideoTypes { get; }
        string MediaServiceAccountName { get; }
        string MediaServiceAccountKey { get; }
        string AzureStorageConnectionString { get; }
    }
}