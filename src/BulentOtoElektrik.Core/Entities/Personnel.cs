namespace BulentOtoElektrik.Core.Entities;

public class Personnel : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? TcKimlikNo { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; } = true;
}
