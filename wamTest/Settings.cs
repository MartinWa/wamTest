using System.Collections.Generic;

namespace wamTest
{
    public class Settings : ISettings
    {
        public IEnumerable<string> SupportedVideoTypes => new List<string> { ".mp4" }; //".3gp,.3g2,.3gp2,.asf,.avi,.dv,.m2ts,.m2v,.m4a,.mod,.mov,.mp4,.mpeg,.mpg,.mts,.ts,.wmv
        public string MediaServiceAccountName => "devzeromedia";
        public string MediaServiceAccountKey => "6dKcTKDLoeTGYM6K1d6hmlGRMCadKTfbPKjd02pgENI=";
        public string AzureStorageConnectionString => "DefaultEndpointsProtocol=https;AccountName=devzerostorage;AccountKey=W14kuJudTLiyOCbTMALw4dsnx0YVjQKeYhx2ZQYAu56/0DZgvqz5AU/0D5RcNLbTrsIXbpZXeEjPrG1CqZ2n6g==";
    }
}