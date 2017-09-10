using System.IO;
using System.Threading.Tasks;

namespace wamTest
{
    public interface IBlob
    {
        Task<bool> ExistsAsync();
        Task<long> GetSizeAsync();
        string GetName();
        Task<string> GetPathAsync();
        Task<bool> CopyBlobAsync(IBlob original);
        Task UploadTextAsync(string text);
        Task UploadFromStreamAsync(Stream stream);
    }
}