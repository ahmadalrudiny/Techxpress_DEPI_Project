using TechXpress.Services.Models;

namespace TechXpress.Services.Interfaces;

public interface IVendorDocumentStorage
{
    /// <summary>Validates and saves a document outside wwwroot. Returns the stored (GUID) name.</summary>
    Task<DocumentSaveResult> SaveAsync(UploadedFile file);

    /// <summary>Opens a stored document for reading, or null if it does not exist.</summary>
    StoredDocument? Open(string storedName);

    void Delete(string storedName);
}