using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DATN.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Services;

/// <summary>
/// Storage Service sử dụng Azure Blob Storage
/// </summary>
public class AzureBlobStorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _connectionString;
    private readonly string _containerName;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        _connectionString = _configuration["AzureBlob:ConnectionString"] ?? throw new ArgumentNullException("AzureBlob:ConnectionString is missing");
        _containerName = _configuration["AzureBlob:ContainerName"] ?? throw new ArgumentNullException("AzureBlob:ContainerName is missing");
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            // Tạo tên file ngẫu nhiên để không bị trùng
            var uniqueFileName = $"{Guid.NewGuid()}-{Path.GetFileName(fileName)}";
            var blobClient = blobContainerClient.GetBlobClient(uniqueFileName);

            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders }, cancellationToken);

            _logger.LogInformation("File {FileName} uploaded successfully to Azure Blob", fileName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to Azure Blob", fileName);
            throw; // Ném ngoại lệ để controller/handler xử lý (trả về lỗi 400 hoặc 500)
        }
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        try
        {
            var uri = new Uri(fileUrl);
            var blobName = Path.GetFileName(uri.LocalPath);

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("File {FileUrl} deleted successfully from Azure Blob", fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileUrl} from Azure Blob", fileUrl);
        }
    }
}
