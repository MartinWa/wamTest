using System;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace wamTest
{
    public class AzureStorage : IStorage
    {
        private readonly Lazy<CloudBlobClient> _client;

        public AzureStorage(ISettings settings)
        {
            var storageAccount = CloudStorageAccount.Parse(settings.AzureStorageConnectionString);
            _client = new Lazy<CloudBlobClient>(() => storageAccount.CreateCloudBlobClient());
        }
        public IContainer GetContainer(string containerName)
        {
            var regex = new Regex("^(?-i)(?:[a-z0-9]|(?<=[0-9a-z])-(?=[0-9a-z])){3,63}$", RegexOptions.Compiled);
            if (!regex.IsMatch(containerName))
            {
                throw new ArgumentException("Container names must conform to these rules: " +
                                            "Must start with a letter or number, and can contain only letters, numbers, and the dash (-) character. " +
                                            "Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names. " +
                                            "All letters in a container name must be lowercase. " +
                                            "Must be from 3 to 63 characters long.");
            }
            return new AzureContainer(_client.Value, containerName);
        }
    }
}


