namespace ConsultancyManagement.Core.Entities;

/// <summary>In-app notification delivered to a single user (consultant, management, admin, sales).</summary>
public class UserNotification
{
    public int Id { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>Machine-readable kind, e.g. DocumentFromManagement, DocumentPendingReview.</summary>
    public string Kind { get; set; } = string.Empty;
    public int? RelatedDocumentId { get; set; }
    public int? RelatedOnboardingTaskId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
