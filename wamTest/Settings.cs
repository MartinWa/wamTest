using System.Collections.Generic;

namespace wamTest
{
    public class Settings : ISettings
    {
        public IEnumerable<string> SupportedVideoTypes => new List<string>();
        public string MediaServiceAccountName => "";
        public string MediaServiceAccountKey => "";
        public string AzureStorageConnectionString => "";
    }
}