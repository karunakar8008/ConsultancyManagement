namespace ConsultancyManagement.Core.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public int? RelatedDocumentId { get; set; }
    public int? RelatedOnboardingTaskId { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}
