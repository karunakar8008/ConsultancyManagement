namespace ConsultancyManagement.Core.Entities;

/// <summary>Consultant-logged vendor reach-out; counts roll into daily activity vendor reach metrics.</summary>
public class ConsultantVendorReachOut
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public DateTime ReachedDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Consultant Consultant { get; set; } = null!;
}
