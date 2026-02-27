namespace BulentOtoElektrik.Core.Entities;

public class Technician : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
}
