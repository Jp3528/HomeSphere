using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeSphere.Infrastructure.Services;

public class LocalStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalStorageService> _logger;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".svg", ".avif"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new BusinessRuleValidationException("File stream cannot be empty.");
        }

        if (fileStream.Length > MaxFileSize)
        {
            throw new BusinessRuleValidationException("File size exceeds the 10 MB limit.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new BusinessRuleValidationException($"File extension '{extension}' is not allowed. Supported formats: .jpg, .jpeg, .png, .webp, .svg, .avif");
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var targetFilePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var outputStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(outputStream, cancellationToken);
        }

        _logger.LogInformation("Stored file {OriginalName} as {UniqueName} in local storage", fileName, uniqueFileName);

        return $"/uploads/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        try
        {
            var fileName = Path.GetFileName(fileUrl);
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads");
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file {FileName} from local storage", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {FileUrl}", fileUrl);
        }

        return Task.CompletedTask;
    }
}
