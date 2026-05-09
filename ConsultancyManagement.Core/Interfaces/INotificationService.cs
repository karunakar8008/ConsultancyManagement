using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetForCurrentUserAsync(string userId, int take = 50);
    Task<int> GetUnreadCountAsync(string userId);
    Task<(bool Ok, string? Error)> MarkReadAsync(string userId, int notificationId);
    Task MarkAllReadAsync(string userId);

    Task NotifyManagementUploadedDocumentAsync(int consultantId, int documentId, string documentType, string fileName);
    Task NotifyConsultantUploadedDocumentPendingReviewAsync(int consultantId, int documentId, string documentType, string fileName);
    Task NotifyDocumentReviewedAsync(int consultantId, int documentId, string fileName, string status);
    Task NotifyOnboardingTaskAssignedAsync(int consultantId, int taskId, string taskName);
}
