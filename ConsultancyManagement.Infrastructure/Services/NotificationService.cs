using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConsultancyManagement.Infrastructure.Services;

public class NotificationService : INotificationService
{
    public const string KindDocumentFromManagement = "DocumentFromManagement";
    public const string KindDocumentPendingReview = "DocumentPendingReview";
    public const string KindDocumentReviewed = "DocumentReviewed";
    public const string KindOnboardingTaskAssigned = "OnboardingTaskAssigned";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForCurrentUserAsync(string userId, int take = 50)
    {
        var rows = await _db.Set<UserNotification>()
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Kind = n.Kind,
                CreatedAt = n.CreatedAt,
                IsRead = n.ReadAt != null,
                RelatedDocumentId = n.RelatedDocumentId,
                RelatedOnboardingTaskId = n.RelatedOnboardingTaskId
            })
            .ToListAsync();
        return rows;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _db.Set<UserNotification>()
            .CountAsync(n => n.RecipientUserId == userId && n.ReadAt == null);
    }

    public async Task<(bool Ok, string? Error)> MarkReadAsync(string userId, int notificationId)
    {
        var n = await _db.Set<UserNotification>()
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientUserId == userId);
        if (n is null) return (false, "Notification not found.");
        if (n.ReadAt is null) n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var now = DateTime.UtcNow;
        await _db.Set<UserNotification>()
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
    }

    public async Task NotifyManagementUploadedDocumentAsync(int consultantId, int documentId, string documentType, string fileName)
    {
        var c = await _db.Consultants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == consultantId);
        if (c is null) return;

        var title = "New document from management";
        var message = $"{documentType}: {fileName}";
        await AddAsync(c.UserId, title, message, KindDocumentFromManagement, documentId, null);
    }

    public async Task NotifyConsultantUploadedDocumentPendingReviewAsync(int consultantId, int documentId, string documentType, string fileName)
    {
        var c = await _db.Consultants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == consultantId);
        if (c is null) return;

        var consultantLabel = $"{c.FirstName} {c.LastName}".Trim();
        var title = "Document pending review";
        var message = $"{consultantLabel} uploaded {documentType}: {fileName}";

        var recipientIds = await CollectStaffAndSalesRecipientIdsAsync(consultantId);
        await AddManyAsync(recipientIds, title, message, KindDocumentPendingReview, documentId, null);
    }

    public async Task NotifyDocumentReviewedAsync(int consultantId, int documentId, string fileName, string status)
    {
        var c = await _db.Consultants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == consultantId);
        if (c is null) return;

        var title = "Document reviewed";
        var message = $"“{fileName}” is now {status}.";
        await AddAsync(c.UserId, title, message, KindDocumentReviewed, documentId, null);
    }

    public async Task NotifyOnboardingTaskAssignedAsync(int consultantId, int taskId, string taskName)
    {
        var c = await _db.Consultants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == consultantId);
        if (c is null) return;

        var title = "New onboarding task";
        var message = taskName;
        await AddAsync(c.UserId, title, message, KindOnboardingTaskAssigned, null, taskId);
    }

    private async Task<HashSet<string>> CollectStaffAndSalesRecipientIdsAsync(int consultantId)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var orgId = await _db.Consultants.AsNoTracking()
            .Where(c => c.Id == consultantId)
            .Select(c => (int?)c.OrganizationId)
            .FirstOrDefaultAsync();
        if (!orgId.HasValue) return ids;

        var mgmtIds = await _db.ManagementUsers.AsNoTracking()
            .Where(m => m.OrganizationId == orgId.Value)
            .Select(m => m.UserId)
            .ToListAsync();
        foreach (var id in mgmtIds)
            ids.Add(id);

        var admins = await _userManager.GetUsersInRoleAsync(UserRole.Admin.ToString());
        foreach (var u in admins)
            if (!u.IsDeleted && u.OrganizationId == orgId.Value)
                ids.Add(u.Id);

        var salesUserIds = await _db.ConsultantSalesAssignments.AsNoTracking()
            .Where(a => a.ConsultantId == consultantId && a.IsActive)
            .Select(a => a.SalesRecruiter.UserId)
            .Distinct()
            .ToListAsync();
        foreach (var id in salesUserIds)
            ids.Add(id);

        return ids;
    }

    private async Task AddAsync(string recipientUserId, string title, string message, string kind, int? documentId, int? onboardingTaskId)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId)) return;
        _db.Set<UserNotification>().Add(new UserNotification
        {
            RecipientUserId = recipientUserId,
            Title = title,
            Message = message,
            Kind = kind,
            RelatedDocumentId = documentId,
            RelatedOnboardingTaskId = onboardingTaskId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task AddManyAsync(IEnumerable<string> recipientUserIds, string title, string message, string kind, int? documentId, int? onboardingTaskId)
    {
        var now = DateTime.UtcNow;
        foreach (var uid in recipientUserIds.Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(uid)) continue;
            _db.Set<UserNotification>().Add(new UserNotification
            {
                RecipientUserId = uid,
                Title = title,
                Message = message,
                Kind = kind,
                RelatedDocumentId = documentId,
                RelatedOnboardingTaskId = onboardingTaskId,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync();
    }
}
