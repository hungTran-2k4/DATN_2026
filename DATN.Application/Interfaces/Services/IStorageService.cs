using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces.Services;

/// <summary>
/// Interface cho Storage Service (Upload/Xóa tệp)
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Upload file lên Storage
    /// </summary>
    /// <param name="fileStream">Stream của file</param>
    /// <param name="fileName">Tên file gốc</param>
    /// <param name="contentType">Loại file (MIME type)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL của file sau khi upload</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa file trên Storage
    /// </summary>
    /// <param name="fileUrl">URL của file cần xóa</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
