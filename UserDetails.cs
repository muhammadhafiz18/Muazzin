using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI
{
    public class UserDetails
    {
        string jsonUrl = "https://muazzinresources.blob.core.windows.net/userdetails/UserDetails.json";
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string blobConnectionString = "DefaultEndpointsProtocol=https;AccountName=muazzinresources;AccountKey=IWrw0RDqCRVgLC/Vcd3PlMzl4eCxCQtxjPBkPsMps3wrnxfkAlX+vLTX+/BRxzY4MO07mLtfBhWH+AStKyIFXg==;EndpointSuffix=core.windows.net";
        private readonly string containerName = "userdetails";
        private readonly string blobName = "UserDetails.json";

        public UserDetails()
        {
            _blobServiceClient = new BlobServiceClient(blobConnectionString);
        }

        public async Task<List<Chat>> UserDetailGetter()
        {
            var response = await _httpClient.GetAsync(jsonUrl);

            // Read the JSON content directly from the response
            var jsonString = await response.Content.ReadAsStringAsync();
            var existingData = JsonConvert.DeserializeObject<List<Chat>>(jsonString) ?? new List<Chat>();

            return existingData;
        }

        public async Task UserDetailAdder(Chat userDetails)
        {
            var response = await _httpClient.GetAsync(jsonUrl);

            var jsonString = await response.Content.ReadAsStringAsync();
            var existingData = JsonConvert.DeserializeObject<List<Chat>>(jsonString) ?? new List<Chat>();

            existingData.Add(userDetails);

            var updatedJson = JsonConvert.SerializeObject(existingData, Formatting.Indented);

            // Step 3: Upload the updated JSON back to Azure Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(updatedJson)))
            {
                await blobClient.UploadAsync(uploadStream, overwrite: true);
            }
        }

        public async Task UserDetailRemover(Chat userDetailToRemove)
        {
            var response = await _httpClient.GetAsync(jsonUrl);

            var jsonString = await response.Content.ReadAsStringAsync();
            var existingData = JsonConvert.DeserializeObject<List<Chat>>(jsonString) ?? new List<Chat>();

            var chatToRemove = existingData.FirstOrDefault(c => c.UserID == userDetailToRemove.UserID);

            existingData.Remove(chatToRemove);

            var updatedJson = JsonConvert.SerializeObject(existingData, Formatting.Indented);

            // Step 3: Upload the updated JSON back to Azure Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(updatedJson)))
            {
                await blobClient.UploadAsync(uploadStream, overwrite: true);
            }
        }
    }
}
