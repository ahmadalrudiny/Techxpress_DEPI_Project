using Microsoft.Extensions.Logging;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

public class VendorDocumentStorage : IVendorDocumentStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;   // 5 MB

    // الامتداد + نوع المحتوى + أول بايتات الملف (Magic Bytes)
    private static readonly Dictionary<string, (string Mime, byte[] Magic)> Allowed =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }),
            [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 })
        };

    private readonly string _root;
    private readonly ILogger<VendorDocumentStorage> _logger;

    public VendorDocumentStorage(string rootPath, ILogger<VendorDocumentStorage> logger)
    {
        _root = Path.GetFullPath(rootPath);
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public async Task<DocumentSaveResult> SaveAsync(UploadedFile file)
    {
        if (file.Length <= 0)
            return new(false, null, "الملف فارغ.");
        if (file.Length > MaxBytes)
            return new(false, null, "حجم الملف يجب ألا يزيد عن 5 ميجابايت.");

        var ext = Path.GetExtension(file.FileName);
        if (!Allowed.TryGetValue(ext, out var info))
            return new(false, null, "الأنواع المسموحة: PDF أو JPG أو PNG.");

        // تأكد إن محتوى الملف فعلاً من النوع اللي امتداده بيقوله
        var header = new byte[info.Magic.Length];
        var read = await file.Content.ReadAtLeastAsync(header.AsMemory(), header.Length, throwOnEndOfStream: false);
        if (read < header.Length || !header.SequenceEqual(info.Magic))
            return new(false, null, "محتوى الملف لا يطابق نوعه.");

        var storedName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var path = Path.Combine(_root, storedName);

        await using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await fs.WriteAsync(header.AsMemory(0, read));
            await file.Content.CopyToAsync(fs);
        }

        // الحجم الحقيقي بعد الكتابة (مش اللي العميل قاله)
        if (new FileInfo(path).Length > MaxBytes)
        {
            File.Delete(path);
            return new(false, null, "حجم الملف يجب ألا يزيد عن 5 ميجابايت.");
        }

        return new(true, storedName, null);
    }

    public StoredDocument? Open(string storedName)
    {
        var full = Resolve(storedName);
        if (full == null || !File.Exists(full)) return null;
        if (!Allowed.TryGetValue(Path.GetExtension(full), out var info)) return null;

        return new StoredDocument(
            new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read), info.Mime);
    }

    public void Delete(string storedName)
    {
        try
        {
            var full = Resolve(storedName);
            if (full != null && File.Exists(full)) File.Delete(full);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete stored document {Name}", storedName);
        }
    }

    // يمنع Path Traversal (مثلاً ../../appsettings.json)
    private string? Resolve(string storedName)
    {
        var name = Path.GetFileName(storedName);
        var full = Path.GetFullPath(Path.Combine(_root, name));
        return full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? full : null;
    }
}