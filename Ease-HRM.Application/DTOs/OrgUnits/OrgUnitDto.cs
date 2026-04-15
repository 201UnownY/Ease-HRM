namespace Ease_HRM.Application.DTOs.OrgUnits;

public class OrgUnitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentOrgUnitId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
}