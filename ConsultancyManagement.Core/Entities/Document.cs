namespace ConsultancyManagement.Core.Entities;

public class Document
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }

    public Consultant Consultant { get; set; } = null!;
}
