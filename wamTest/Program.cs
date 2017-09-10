using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wamTest
{
    internal static class Program
    {
        private const string JobIdentifier = "Encoding {0} in content id {1}";

        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            var settings = new Settings();
            var storage = new AzureStorage(settings);
            var mediaService = new AzureMediaService(settings, storage, new AzureMediaServiceFactory(settings));
            const int contentId = 22;
            var fileGuid = Guid.NewGuid().ToString("N").ToLower();
            var newFileName = $"{fileGuid}{mediaService.EncodedFileExtension()}";
            string jobId;
            using (var fileStream = new FileStream("movie.mp4", FileMode.Open, FileAccess.Read))
            {

                var fileExtension = Path.GetExtension(fileStream.Name);
                var originalBackupFileName = $"{fileGuid}_original{fileExtension}";
                var originalBlob = storage.GetContainer(contentId.ToString()).GetBlob(originalBackupFileName);
                await originalBlob.UploadFromStreamAsync(fileStream);
                var encodedBlob = storage.GetContainer(contentId.ToString()).GetBlob(newFileName);
                await encodedBlob.UploadTextAsync(""); // Create empty blob
                var jobIdentifier = string.Format(JobIdentifier, newFileName, contentId);
                var job = await mediaService.CreateEncodeJobAsync(originalBlob, encodedBlob.GetName(), jobIdentifier, CancellationToken.None);
                jobId = job.Id;
            }
            var message = new CompleteMediaEncodingQueueMessageDto
            {
                JobIdentifier = jobId,
                ContentId = contentId,
                NewFileName = newFileName
            };
            Console.WriteLine("Working on encoding job id {0}, contentId {1}, filename {2}", message.JobIdentifier, message.ContentId, message.NewFileName);
            await mediaService.FinishEncodeJobAsync(message.JobIdentifier, message.ContentId, message.NewFileName, CancellationToken.None);
            Console.WriteLine("Done with encoding job id {0}, contentId {1}, filename {2}", message.JobIdentifier, message.ContentId, message.NewFileName);
        }
    }
}