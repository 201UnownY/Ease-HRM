namespace Ease_HRM.Application.DTOs.OrgUnits;

public class CreateOrgUnitRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentOrgUnitId { get; set; }
    public int Level { get; set; }
}