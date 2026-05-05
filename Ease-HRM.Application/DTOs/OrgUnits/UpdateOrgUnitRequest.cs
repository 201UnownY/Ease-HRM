namespace Ease_HRM.Application.DTOs.OrgUnits;

public class UpdateOrgUnitRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
