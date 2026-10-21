using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Implementations;
using TechXpress.Services.Models;

namespace TechXpress.Services.Tests;

public class VendorDocumentStorageTests : IDisposable
{
    private readonly string _rootPath;
    private readonly VendorDocumentStorage _storage;
    private readonly Mock<ILogger<VendorDocumentStorage>> _loggerMock;

    public VendorDocumentStorageTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), $"TechXpressTests_{Guid.NewGuid():N}");
        _loggerMock = new Mock<ILogger<VendorDocumentStorage>>();
        _storage = new VendorDocumentStorage(_rootPath, _loggerMock.Object);
    }

    private static UploadedFile CreateFile(string fileName, byte[] content, string? contentType = null)
    {
        return new UploadedFile(new MemoryStream(content), fileName, content.Length);
    }

    [Fact]
    public async Task SaveAsync_ValidPdf_ReturnsStoredName()
    {
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x01, 0x02, 0x03 };
        var file = CreateFile("document.pdf", pdfContent);

        var result = await _storage.SaveAsync(file);

        Assert.True(result.Success);
        Assert.NotNull(result.StoredName);
        Assert.EndsWith(".pdf", result.StoredName);
        Assert.Null(result.Error);
        Assert.True(File.Exists(Path.Combine(_rootPath, result.StoredName!)));
    }

    [Fact]
    public async Task SaveAsync_ValidJpg_ReturnsStoredName()
    {
        var jpgContent = new byte[] { 0xFF, 0xD8, 0xFF, 0x01, 0x02 };
        var file = CreateFile("photo.jpg", jpgContent);

        var result = await _storage.SaveAsync(file);

        Assert.True(result.Success);
        Assert.NotNull(result.StoredName);
        Assert.EndsWith(".jpg", result.StoredName);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SaveAsync_EmptyFile_ReturnsError()
    {
        var file = CreateFile("empty.pdf", Array.Empty<byte>());

        var result = await _storage.SaveAsync(file);

        Assert.False(result.Success);
        Assert.Null(result.StoredName);
        Assert.Equal("الملف فارغ.", result.Error);
    }

    [Fact]
    public async Task SaveAsync_InvalidExtension_ReturnsError()
    {
        var file = CreateFile("document.txt", new byte[] { 0x01, 0x02, 0x03 });

        var result = await _storage.SaveAsync(file);

        Assert.False(result.Success);
        Assert.Null(result.StoredName);
        Assert.Contains("الأنواع المسموحة", result.Error);
    }

    [Fact]
    public async Task SaveAsync_WrongMagicBytes_ReturnsError()
    {
        // امتداد PDF لكن المحتوى ليس PDF
        var file = CreateFile("fake.pdf", new byte[] { 0x00, 0x00, 0x00, 0x00 });

        var result = await _storage.SaveAsync(file);

        Assert.False(result.Success);
        Assert.Null(result.StoredName);
        Assert.Contains("محتوى الملف", result.Error);
    }

    [Fact]
    public void Open_ExistingFile_ReturnsDocument()
    {
        var storedName = Guid.NewGuid().ToString("N") + ".pdf";
        var fullPath = Path.Combine(_rootPath, storedName);
        File.WriteAllBytes(fullPath, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });

        var doc = _storage.Open(storedName);

        Assert.NotNull(doc);
        Assert.Equal("application/pdf", doc.ContentType);
        doc.Content.Dispose();
    }

    [Fact]
    public void Open_NonExistingFile_ReturnsNull()
    {
        var doc = _storage.Open("nonexistent.pdf");
        Assert.Null(doc);
    }

    [Fact]
    public void Delete_ExistingFile_RemovesFile()
    {
        var storedName = Guid.NewGuid().ToString("N") + ".pdf";
        var fullPath = Path.Combine(_rootPath, storedName);
        File.WriteAllBytes(fullPath, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });

        _storage.Delete(storedName);

        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public void Delete_NonExistingFile_DoesNotThrow()
    {
        // لا يجب أن يلقي استثناء حتى لو الملف غير موجود
        _storage.Delete("nonexistent.pdf");
        Assert.True(true);
    }

    [Fact]
    public async Task SaveAsync_FileExceedsMaxSize_ReturnsError()
    {
        var largeContent = new byte[6 * 1024 * 1024]; // 6 MB
        new Random().NextBytes(largeContent);
        var file = CreateFile("large.pdf", largeContent);

        //'ajoute PDF magic bytes at start
        largeContent[0] = 0x25; largeContent[1] = 0x50; largeContent[2] = 0x44; largeContent[3] = 0x46; largeContent[4] = 0x2D;

        var result = await _storage.SaveAsync(file);

        Assert.False(result.Success);
        Assert.Null(result.StoredName);
        Assert.Contains("5 ميجابايت", result.Error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            try { Directory.Delete(_rootPath, recursive: true); } catch { }
        }
    }
}
