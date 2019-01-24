using Contoso.Events.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Contoso.Events.Data
{
    public class BlobContext
    {
        protected StorageSettings StorageSettings;

        public BlobContext(IOptions<StorageSettings> cosmosSettings)
        {
            StorageSettings = cosmosSettings.Value;
        }

        public async Task<ICloudBlob> UploadBlobAsync(string blobName, Stream stream)
        {
            var account = CloudStorageAccount.Parse(StorageSettings.ConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference($"{StorageSettings.ContainerName}-pending");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(blobName);
            stream.Seek(0, SeekOrigin.Begin);
            await blob.UploadFromStreamAsync(stream);
            return blob;
        }

        public async Task<DownloadPayload> GetStreamAsync(string blobId)
        {
            var account = CloudStorageAccount.Parse(StorageSettings.ConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(StorageSettings.ContainerName);
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlockBlobReference(blobId);
            var blobStream = await blob.OpenReadAsync(null, null, null);

            return new DownloadPayload { Stream = blobStream, ContentType = blob.Properties.ContentType };
        }

        public async Task<string> GetSecureUrlAsync(string blobId)
        {
            var account = CloudStorageAccount.Parse(StorageSettings.ConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(StorageSettings.ContainerName);
            await container.CreateIfNotExistsAsync();

            var blobPolicy = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.Now.AddHours(0.25d),
                Permissions = SharedAccessBlobPermissions.Read
            };

            var blobPermissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Off
            };
            blobPermissions.SharedAccessPolicies.Add("ReadBlobPolicy", blobPolicy);

            await container.SetPermissionsAsync(blobPermissions);

            var sasToken = container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), "ReadBlobPolicy");

            var blob = container.GetBlockBlobReference(blobId);
            var blobUrl = blob.Uri;

            return blobUrl.AbsoluteUri + sasToken;
        }
    }
}