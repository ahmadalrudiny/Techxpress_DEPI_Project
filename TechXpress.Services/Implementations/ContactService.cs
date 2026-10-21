using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechXpress.Data.Entities;
using TechXpress.Data.UoW;
using TechXpress.Services.Interfaces;
using TechXpress.Services.Models;

namespace TechXpress.Services.Implementations;

/// <inheritdoc cref="IContactService" />
public class ContactService : IContactService
{
    private const int MaxNameLength = 150;
    private const int MaxEmailLength = 256;
    private const int MaxPhoneLength = 30;
    private const int MaxSubjectLength = 200;
    private const int MaxMessageLength = 2000;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ContactService> _logger;

    public ContactService(IUnitOfWork unitOfWork, ILogger<ContactService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendAsync(
        string fullName, string email, string? phone, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(message))
        {
            return (false, "برجاء إكمال جميع الحقول المطلوبة.");
        }

        try
        {
            await _unitOfWork.ContactMessages.AddAsync(new ContactMessage
            {
                FullName = Clamp(fullName.Trim(), MaxNameLength),
                Email = Clamp(email.Trim(), MaxEmailLength),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : Clamp(phone.Trim(), MaxPhoneLength),
                Subject = Clamp(subject.Trim(), MaxSubjectLength),
                Message = Clamp(message.Trim(), MaxMessageLength)
            });

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Contact message received from {Email}", email);

            return (true, "تم استلام رسالتك بنجاح، وسيتواصل معك فريق الدعم في أقرب وقت.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save contact message from {Email}", email);
            throw;
        }
    }

    public async Task<IReadOnlyList<ContactMessageDto>> GetAllAsync()
    {
        var items = await _unitOfWork.ContactMessages
            .Query()
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return items.Select(c => new ContactMessageDto(
            c.Id, c.FullName, c.Email, c.Phone, c.Subject, c.Message, c.CreatedAt, c.IsRead)).ToList();
    }

    public Task<int> GetUnreadCountAsync()
        => _unitOfWork.ContactMessages.Query().CountAsync(c => !c.IsRead);

    public async Task<(bool Success, string Message)> MarkAsReadAsync(int id)
    {
        var entity = await _unitOfWork.ContactMessages.GetByIdAsync(id);
        if (entity == null)
        {
            return (false, "الرسالة غير موجودة.");
        }

        if (entity.IsRead)
        {
            return (true, "الرسالة مُعلّمة كمقروءة بالفعل.");
        }

        try
        {
            entity.IsRead = true;
            _unitOfWork.ContactMessages.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return (true, "تم تعليم الرسالة كمقروءة.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark contact message {Id} as read", id);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _unitOfWork.ContactMessages.GetByIdAsync(id);
        if (entity == null)
        {
            return (false, "الرسالة غير موجودة.");
        }

        try
        {
            _unitOfWork.ContactMessages.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Contact message {Id} deleted", id);
            return (true, "تم حذف الرسالة.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete contact message {Id}", id);
            throw;
        }
    }

    private static string Clamp(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
