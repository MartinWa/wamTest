using System;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace wamTest
{
    public class AzureMediaServiceFactory
    {
        private readonly Lazy<CloudMediaContext> _context;

        public AzureMediaServiceFactory(ISettings settings)
        {
            var cachedCredentials = new MediaServicesCredentials(settings.MediaServiceAccountName, settings.MediaServiceAccountKey);
            _context = new Lazy<CloudMediaContext>(() => new CloudMediaContext(cachedCredentials));
        }

        public CloudMediaContext GetCloudMediaContext()
        {
            return _context.Value;
        }
    }
}