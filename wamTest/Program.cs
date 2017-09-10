using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wamTest
{
    internal static class Program
    {
        private const int ContentId = 22;
        private static readonly AzureStorage Storage;
        private static readonly IMediaService MediaService;
        private const string JobIdentifierSchema = "Encoding {0} in content id {1}";

        static Program()
        {
            var settings = new Settings();
            Storage = new AzureStorage(settings);
            MediaService = new AzureMediaService(settings, Storage, new AzureMediaServiceFactory(settings));
        }


        public static void Main()
        {
            var message = UploadAndCreateJob().GetAwaiter().GetResult();
            var task = FinishEncodeJobAsync(message);
            while (!task.IsCompleted)
            {
                var jobIdentifier = string.Format(JobIdentifierSchema, message.NewFileName, ContentId);
                var resultingFile = Storage.GetContainer(ContentId.ToString()).GetBlob(message.NewFileName);
                var progress = MediaService.GetEncodeProgressAsync(jobIdentifier, resultingFile).GetAwaiter().GetResult();
                Console.WriteLine($"JobId: {jobIdentifier}, status: {progress.Status} - {progress.ProgressPercentage}% ({progress.Errors})");
                Thread.Sleep(1000);
            }
        }

        private static async Task<CompleteMediaEncodingQueueMessageDto> UploadAndCreateJob()
        {
            var fileGuid = Guid.NewGuid().ToString("N").ToLower();
            var newFileName = $"{fileGuid}{MediaService.EncodedFileExtension()}";
            string jobId;
            using (var fileStream = new FileStream("movie.mp4", FileMode.Open, FileAccess.Read))
            {
                var fileExtension = Path.GetExtension(fileStream.Name);
                var originalBackupFileName = $"{fileGuid}_original{fileExtension}";
                var originalBlob = Storage.GetContainer(ContentId.ToString()).GetBlob(originalBackupFileName);
                Console.WriteLine($"Uploading file {fileStream.Name} to blob");
                await originalBlob.UploadFromStreamAsync(fileStream);
                Console.WriteLine("Upload done");
                var encodedBlob = Storage.GetContainer(ContentId.ToString()).GetBlob(newFileName);
                await encodedBlob.UploadTextAsync(""); // Create empty blob
                var jobIdentifier = string.Format(JobIdentifierSchema, newFileName, ContentId);
                Console.WriteLine($"Starting encode job {jobIdentifier}");
                var job = await MediaService.CreateEncodeJobAsync(originalBlob, encodedBlob.GetName(), jobIdentifier, CancellationToken.None);
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
            await MediaService.FinishEncodeJobAsync(message.JobIdentifier, message.ContentId, message.NewFileName, CancellationToken.None);
            Console.WriteLine($"Done with encoding job id {message.JobIdentifier}, contentId {message.ContentId}, filename {message.NewFileName}");
        }
    }
}