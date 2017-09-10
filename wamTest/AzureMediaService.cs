using System;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace wamTest
{
    public class AzureMediaService : IMediaService
    {
        private readonly IStorage _storage;
        private readonly ISettings _settings;
        private readonly Lazy<CloudMediaContext> _cloudMediaContext;
        private const string EncodedFileExtensionn = ".mp4";

        public AzureMediaService(ISettings settings, IStorage storage, AzureMediaServiceFactory factory)
        {
            _settings = settings;
            _storage = storage;
            _cloudMediaContext = new Lazy<CloudMediaContext>(factory.GetCloudMediaContext);
        }

        public async Task<IJob> CreateEncodeJobAsync(IBlob original, string encodedFileName, string jobIdentifier, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(original.GetName());
            if (extension == null || !_settings.SupportedVideoTypes.Contains(extension.ToLower()))
            {
                throw new NotSupportedException("Video type not supported");
            }
            var assetName = Guid.NewGuid().ToString();
            var asset = await _cloudMediaContext.Value.Assets.CreateAsync(assetName, AssetCreationOptions.None, cancellationToken);
            var writePolicy = await _cloudMediaContext.Value.AccessPolicies.CreateAsync("writePolicy", TimeSpan.FromHours(24), AccessPermissions.Write);
            var destinationLocator = await _cloudMediaContext.Value.Locators.CreateLocatorAsync(LocatorType.Sas, asset, writePolicy);
            var assetContainer = _storage.GetContainer((new Uri(destinationLocator.Path)).Segments[1]);
            var ingestedAssetFile = await asset.AssetFiles.CreateAsync(original.GetName(), cancellationToken);
            await assetContainer.GetBlob(ingestedAssetFile.Name).CopyBlobAsync(original);
            var path = await original.GetPathAsync();
            var job = _cloudMediaContext.Value.Jobs.Create(string.Format(jobIdentifier, path, original.GetName()));
            var encoder = _cloudMediaContext.Value.MediaProcessors.GetLatestMediaProcessorByName(MediaProcessorNames.MediaEncoderStandard);
            var task = job.Tasks.AddNew(jobIdentifier, encoder, "H264 Single Bitrate 720p", TaskOptions.None);
            task.InputAssets.Add(asset);
            task.OutputAssets.AddNew(encodedFileName, AssetCreationOptions.None);
            await job.SubmitAsync();
            return job;
        }

        public async Task FinishEncodeJobAsync(string jobIdentifier, int contentId, string newFilename, CancellationToken cancellationToken)
        {
            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault Direct FirstOrDefault not supported
            var job = _cloudMediaContext.Value.Jobs.Where(j => j.Id == jobIdentifier).FirstOrDefault();
            if (job == null)
            {
                throw new InstanceNotFoundException($"No job with id {jobIdentifier} was found");
            }
            var encoded = _storage.GetContainer(contentId.ToString()).GetBlob(newFilename);
            var progressJobTask = job.GetExecutionProgressTask(cancellationToken);
            await progressJobTask;
            if (job.State == JobState.Error)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Error while encoding job {job.Name}");
                foreach (var task in job.Tasks)
                {
                    foreach (var detail in task.ErrorDetails)
                    {
                        sb.AppendLine($"Task {task.Name}: error {detail.Message}");
                    }
                }
                throw new Exception(sb.ToString());
            }
            var outputAsset = job.OutputMediaAssets.FirstOrDefault();
            if (outputAsset != null)
            {
                var readPolicy = await _cloudMediaContext.Value.AccessPolicies.CreateAsync("readPolicy", TimeSpan.FromHours(24), AccessPermissions.Read);
                var encodedLocator = await _cloudMediaContext.Value.Locators.CreateLocatorAsync(LocatorType.Sas, outputAsset, readPolicy);
                var encodedassetContainer = _storage.GetContainer((new Uri(encodedLocator.Path)).Segments[1]);
                // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault Direct FirstOrDefault not supported
                var videoAsset = outputAsset.AssetFiles.Where(assetFile => assetFile.MimeType == "video/mp4").FirstOrDefault();
                if (videoAsset != null)
                {
                    await encoded.CopyBlobAsync(encodedassetContainer.GetBlob(videoAsset.Name));
                }
                await encodedLocator.DeleteAsync();
                await readPolicy.DeleteAsync();
            }
            foreach (var asset in job.InputMediaAssets)
            {
                await asset.DeleteAsync();
            }
            foreach (var asset in job.OutputMediaAssets)
            {
                await asset.DeleteAsync();
            }
            //   await job.DeleteAsync();
        }

        public async Task<MediaEncodeProgressDto> GetEncodeProgressAsync(string jobIdentifier, IBlob resultingFile)
        {
            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault Direct FirstOrDefault not supported
            var job = _cloudMediaContext.Value.Jobs.Where(j => j.Name == jobIdentifier).FirstOrDefault();
            if (job == null)
            {
                return new MediaEncodeProgressDto
                {
                    Status = EncodeStatus.NotFound,
                    ProgressPercentage = 0,
                    Errors = "Not found"
                };
            }
            var task = job.Tasks.FirstOrDefault();
            var status = ConvertToEncodeStatus(job.State);
            if (status == EncodeStatus.Finished)
            {
                var exists = await resultingFile.ExistsAsync();
                var size = await resultingFile.GetSizeAsync();
                if (exists && size < 1)
                {
                    status = EncodeStatus.Copying;
                }
            }
            return new MediaEncodeProgressDto
            {
                Status = status,
                ProgressPercentage = task?.Progress ?? 0,
                Errors = task == null ? string.Empty : string.Concat(task.ErrorDetails.Select(ed => ed.Message))
            };
        }

        private static EncodeStatus ConvertToEncodeStatus(JobState state)
        {
            switch (state)
            {
                case JobState.Queued:
                    return EncodeStatus.Queued;
                case JobState.Scheduled:
                    return EncodeStatus.Scheduled;
                case JobState.Processing:
                    return EncodeStatus.Processing;
                case JobState.Finished:
                    return EncodeStatus.Finished;
                case JobState.Error:
                    return EncodeStatus.Error;
                case JobState.Canceled:
                    return EncodeStatus.Canceled;
                case JobState.Canceling:
                    return EncodeStatus.Canceling;
                default:
                    return EncodeStatus.NotFound;
            }
        }

        public string EncodedFileExtension()
        {
            return EncodedFileExtensionn;
        }
    }
}