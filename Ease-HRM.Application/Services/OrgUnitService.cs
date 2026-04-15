using Ease_HRM.Application.DTOs.OrgUnits;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class OrgUnitService : IOrgUnitService
{
    private readonly IOrgUnitRepository _orgUnitRepository;

    public OrgUnitService(IOrgUnitRepository orgUnitRepository)
    {
        _orgUnitRepository = orgUnitRepository;
    }

    public async Task<OrgUnitDto> CreateOrgUnitAsync(CreateOrgUnitRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("OrgUnit name is required.");
        }

        if (await _orgUnitRepository.NameExistsAsync(normalizedName, cancellationToken))
        {
            throw new InvalidOperationException("OrgUnit name already exists.");
        }

        if (request.ParentOrgUnitId.HasValue && request.ParentOrgUnitId != Guid.Empty)
        {
            if (!await _orgUnitRepository.ExistsAsync(request.ParentOrgUnitId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Parent OrgUnit not found.");
            }
        }

        var orgUnit = new OrgUnit
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            ParentOrgUnitId = request.ParentOrgUnitId,
            Level = request.Level,
            IsActive = true
        };

        await _orgUnitRepository.AddAsync(orgUnit, cancellationToken);
        await _orgUnitRepository.SaveChangesAsync(cancellationToken);

        return new OrgUnitDto
        {
            Id = orgUnit.Id,
            Name = orgUnit.Name,
            ParentOrgUnitId = orgUnit.ParentOrgUnitId,
            Level = orgUnit.Level,
            IsActive = orgUnit.IsActive
        };
    }

    public async Task<IReadOnlyList<OrgUnitDto>> GetAllOrgUnitsAsync(CancellationToken cancellationToken = default)
    {
        var orgUnits = await _orgUnitRepository.GetAllAsync(cancellationToken);

        return orgUnits
            .Select(x => new OrgUnitDto
            {
                Id = x.Id,
                Name = x.Name,
                ParentOrgUnitId = x.ParentOrgUnitId,
                Level = x.Level,
                IsActive = x.IsActive
            })
            .ToList()
            .AsReadOnly();
    }
}