namespace ConsultancyManagement.Core.Entities;

/// <summary>Consultant-logged vendor reach-out; counts roll into daily activity vendor reach metrics.</summary>
public class ConsultantVendorReachOut
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public DateTime ReachedDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    /// <summary>When set, this reach-out counts toward daily activity vendor responses for <see cref="ReachedDate"/>.</summary>
    public string? VendorResponseNotes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Consultant Consultant { get; set; } = null!;
}
