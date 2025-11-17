using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace TrustFlow.Core.Services
{
    public class AzureBlobService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobService(IConfiguration configuration)
        {
            string connectionString = configuration["Azure:BlobConnectionString"];
            string containerName = configuration["Azure:ContainerName"];

            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> UploadImageAsync(string userId, IFormFile file)
        {
            string fileName = userId;
            var blobClient = _containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return blobClient.Uri.ToString();
        }
        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                Uri uri = new Uri(imageUrl);
                string blobName = Path.GetFileName(uri.LocalPath);

                var blobClient = _containerClient.GetBlobClient(blobName);
                return await blobClient.DeleteIfExistsAsync();
            }
            catch
            {
                return false;
            }
        }

    }
}
