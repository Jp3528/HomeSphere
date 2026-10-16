using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeSphere.UnitTests.Catalog;

public class LocalStorageServiceTests
{
    private readonly LocalStorageService _service;
    private readonly string _tempPath;

    public LocalStorageServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "HomeSphereTests_" + Guid.NewGuid().ToString("N"));
        var fakeEnv = new FakeWebHostEnvironment(_tempPath);
        _service = new LocalStorageService(fakeEnv, NullLogger<LocalStorageService>.Instance);
    }

    [Fact]
    public async Task UploadFile_WhenDisallowedExtension_ShouldThrowBusinessRuleValidationException()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _service.UploadFileAsync(stream, "virus.exe", "application/x-msdownload"));
    }

    [Fact]
    public async Task UploadFile_WhenEmptyStream_ShouldThrowBusinessRuleValidationException()
    {
        using var emptyStream = new MemoryStream(Array.Empty<byte>());
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _service.UploadFileAsync(emptyStream, "photo.jpg", "image/jpeg"));
    }

    [Fact]
    public async Task UploadFile_WhenValidImage_ShouldReturnUploadUrl()
    {
        using var stream = new MemoryStream(new byte[] { 255, 216, 255, 224 });
        var url = await _service.UploadFileAsync(stream, "living_room.webp", "image/webp");

        Assert.NotNull(url);
        Assert.StartsWith("/uploads/", url);
        Assert.EndsWith(".webp", url);
    }

    private class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string path)
        {
            WebRootPath = path;
            ContentRootPath = path;
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "HomeSphere.WebApi";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Testing";
    }
}
