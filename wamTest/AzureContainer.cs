using System;
using System.Management.Instrumentation;
using Microsoft.WindowsAzure.Storage.Blob;

namespace wamTest
{
    public class AzureContainer : IContainer
    {
        private readonly CloudBlobClient _client;
        private readonly string _containername;

        public AzureContainer(CloudBlobClient client, string containerName)
        {
            _client = client;
            _containername = containerName;
        }

        public IBlob GetBlob(string filename)
        {
            return new AzureBlob(Container, CreateSafeFilename(filename));
        }

        private CloudBlobContainer _container;
        private CloudBlobContainer Container
        {
            get
            {
                if (_container != null)
                {
                    return _container;
                }
                _container = _client.GetContainerReference(_containername);
                //if (_container.Exists())
                //{
                //    return _container;
                //}
                //_container.Create();
                //_container.SetPermissions(new BlobContainerPermissions
                //{
                //    PublicAccess = BlobContainerPublicAccessType.Off
                //});
                return _container;
            }
        }

        private static string CreateSafeFilename(string filename)
        {
            if (filename.Length < 1)
            {
                Console.WriteLine("Filename is to short");
                filename = "none";
            }
            if (filename.Length > 1024) // Blob names must be from 1 to 1024 characters long
            {
                Console.WriteLine($"Filename >{filename}< is to long");
                filename = filename.Substring(0, 1000);
            }
            return filename;
        }
    }
}