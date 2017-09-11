using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wamTest
{
    internal static class Program
    {
        private const int ContentId = 22;
        private static AzureStorage _storage;
        private static IMediaService _mediaService;
        private const string JobIdentifierSchema = "Encoding {0} in content id {1}";
        private const string ContainerName = "test";

        public static void Main()
        {
            var settings = new Settings();
            _storage = new AzureStorage(settings);
            _mediaService = new AzureMediaService(settings, _storage, new AzureMediaServiceFactory(settings));
            var message = UploadAndCreateJob().GetAwaiter().GetResult();
            var task = FinishEncodeJobAsync(message);
            var status = EncodeStatus.NotFound;
            while (status != EncodeStatus.Finished && status != EncodeStatus.Error)
            {
                var jobIdentifier = string.Format(JobIdentifierSchema, message.NewFileName, ContentId);
                var resultingFile = _storage.GetContainer(ContainerName).GetBlob(message.NewFileName);
                var progress = _mediaService.GetEncodeProgressAsync(jobIdentifier, resultingFile).GetAwaiter().GetResult();
                Console.WriteLine($"JobId: {jobIdentifier}, status: {progress.Status} - {progress.ProgressPercentage}% ({progress.Errors})");
                Thread.Sleep(1000);
                status = progress.Status;
            }
            task.GetAwaiter().GetResult();
        }

        private static async Task<CompleteMediaEncodingQueueMessageDto> UploadAndCreateJob()
        {
            var fileGuid = Guid.NewGuid().ToString("N").ToLower();
            var newFileName = $"{fileGuid}{_mediaService.EncodedFileExtension()}";
            var originalBlob = await UploadBlob("movie.mp4");
            var encodedBlob = _storage.GetContainer(ContainerName).GetBlob(newFileName);
            await encodedBlob.UploadTextAsync(""); // Create empty blob
            var jobIdentifier = string.Format(JobIdentifierSchema, newFileName, ContentId);
            Console.WriteLine($"Starting encode job {jobIdentifier}");
            var job = await _mediaService.CreateEncodeJobAsync(originalBlob, encodedBlob.GetName(), jobIdentifier, CancellationToken.None);
            var jobId = job.Id;
            Console.WriteLine($"Encode job {jobId} started");
            var message = new CompleteMediaEncodingQueueMessageDto
            {
                JobIdentifier = jobId,
                ContentId = ContentId,
                NewFileName = newFileName
            };
            return message;
        }

        private static async Task<IBlob> UploadBlob(string file)
        {
            var fileExtension = Path.GetExtension(file);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            var originalBackupFileName = $"{fileNameWithoutExtension}_original{fileExtension}";
            var originalBlob = _storage.GetContainer(ContainerName).GetBlob(originalBackupFileName);
            var size = await originalBlob.GetSizeAsync();
            if (size > 0)
            {
                Console.WriteLine($"File {originalBlob.GetName()} already exists, {size} bytes, returning");
                return originalBlob;
            }
            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine($"Uploading file {fileStream.Name} to blob");
                await originalBlob.UploadFromStreamAsync(fileStream);
                Console.WriteLine($"Upload to {originalBackupFileName} done");
            }
            return originalBlob;
        }

        private static async Task FinishEncodeJobAsync(CompleteMediaEncodingQueueMessageDto message)
        {
            Console.WriteLine($"Working on encoding job id {message.JobIdentifier}, contentId {message.ContentId}, filename {message.NewFileName}");
            await _mediaService.FinishEncodeJobAsync(message.JobIdentifier, message.ContentId, message.NewFileName, CancellationToken.None);
            Console.WriteLine($"Done with encoding job id {message.JobIdentifier}, contentId {message.ContentId}, filename {message.NewFileName}");
        }
    }
}