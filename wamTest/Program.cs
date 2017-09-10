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

        public static void Main()
        {
            var settings = new Settings();
            _storage = new AzureStorage(settings);
            _mediaService = new AzureMediaService(settings, _storage, new AzureMediaServiceFactory(settings));
            var message = UploadAndCreateJob().GetAwaiter().GetResult();
            var task = FinishEncodeJobAsync(message);
            while (!task.IsCompleted)
            {
                var jobIdentifier = string.Format(JobIdentifierSchema, message.NewFileName, ContentId);
                var resultingFile = _storage.GetContainer(ContentId.ToString()).GetBlob(message.NewFileName);
                var progress = _mediaService.GetEncodeProgressAsync(jobIdentifier, resultingFile).GetAwaiter().GetResult();
                Console.WriteLine($"JobId: {jobIdentifier}, status: {progress.Status} - {progress.ProgressPercentage}% ({progress.Errors})");
                Thread.Sleep(1000);
            }
        }

        private static async Task<CompleteMediaEncodingQueueMessageDto> UploadAndCreateJob()
        {
            var fileGuid = Guid.NewGuid().ToString("N").ToLower();
            var newFileName = $"{fileGuid}{_mediaService.EncodedFileExtension()}";
            string jobId;
            using (var fileStream = new FileStream("movie.mp4", FileMode.Open, FileAccess.Read))
            {
                var fileExtension = Path.GetExtension(fileStream.Name);
                var originalBackupFileName = $"{fileGuid}_original{fileExtension}";
                var originalBlob = _storage.GetContainer(ContentId.ToString()).GetBlob(originalBackupFileName);
                Console.WriteLine($"Uploading file {fileStream.Name} to blob");
                await originalBlob.UploadFromStreamAsync(fileStream);
                Console.WriteLine("Upload done");
                var encodedBlob = _storage.GetContainer(ContentId.ToString()).GetBlob(newFileName);
                await encodedBlob.UploadTextAsync(""); // Create empty blob
                var jobIdentifier = string.Format(JobIdentifierSchema, newFileName, ContentId);
                Console.WriteLine($"Starting encode job {jobIdentifier}");
                var job = await _mediaService.CreateEncodeJobAsync(originalBlob, encodedBlob.GetName(), jobIdentifier, CancellationToken.None);
                jobId = job.Id;
                Console.WriteLine($"Encode job {jobId} started");
            }
            var message = new CompleteMediaEncodingQueueMessageDto
            {
                JobIdentifier = jobId,
                ContentId = ContentId,
                NewFileName = newFileName
            };
            return message;
        }
        private static async Task FinishEncodeJobAsync(CompleteMediaEncodingQueueMessageDto message)
        {
            Console.WriteLine($"Working on encoding job id {message.JobIdentifier}, contentId {message.ContentId}, filename {message.NewFileName}");
            await _mediaService.FinishEncodeJobAsync(message.JobIdentifier, message.ContentId, message.NewFileName, CancellationToken.None);
            Console.WriteLine($"Done with encoding job id {message.JobIdentifier}, contentId {message.ContentId}, filename {message.NewFileName}");
        }
    }
}