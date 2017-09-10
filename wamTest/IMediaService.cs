using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace wamTest
{
    public interface IMediaService
    {
        Task<IJob> CreateEncodeJobAsync(IBlob original, string encodedFileName, string jobIdentifier, CancellationToken cancellationToken);
        Task FinishEncodeJobAsync(string jobIdentifier, int contentId, string newFilename,CancellationToken cancellationToken);
        Task<MediaEncodeProgressDto> GetEncodeProgressAsync(string jobIdentifier, IBlob resultingFile);
        string EncodedFileExtension();
    }
}