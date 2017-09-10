using System.Collections.Generic;

namespace wamTest
{
    public interface ISettings
    {
        IEnumerable<string> SupportedVideoTypes { get; set; }
        string MediaServiceAccountName { get; set; }
        string MediaServiceAccountKey { get; set; }
    }
}