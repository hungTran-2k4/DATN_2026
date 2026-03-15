using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using MediatR;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Images.Commands.UploadImage;

public class UploadImageCommand : IRequest<ApiResponse<string>>
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, ApiResponse<string>>
{
    private readonly IStorageService _storageService;

    public UploadImageCommandHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<ApiResponse<string>> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        if (request.FileStream == null || request.FileStream.Length == 0)
        {
            return ApiResponse<string>.Fail("File is empty or not provided.", 400);
        }

        try
        {
            var url = await _storageService.UploadFileAsync(request.FileStream, request.FileName, request.ContentType, cancellationToken);
            return ApiResponse<string>.Succeed(url, "Upload image successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail($"Failed to upload image: {ex.Message}", 500);
        }
    }
}
