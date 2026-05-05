using Ease_HRM.Application.DTOs.OrgUnits;

namespace Ease_HRM.Application.Interfaces;

public interface IOrgUnitService
{
    Task<OrgUnitDto> CreateOrgUnitAsync(CreateOrgUnitRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrgUnitDto>> GetAllOrgUnitsAsync(CancellationToken cancellationToken = default);
    Task<OrgUnitDto> UpdateOrgUnitAsync(UpdateOrgUnitRequest request, CancellationToken cancellationToken = default);
}
