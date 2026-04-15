using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.OrgUnits;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("org-units")]
public class OrgUnitsController : ControllerBase
{
    private readonly IOrgUnitService _orgUnitService;

    public OrgUnitsController(IOrgUnitService orgUnitService)
    {
        _orgUnitService = orgUnitService;
    }

    [HasPermission(Permissions.OrgUnit.Create)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrgUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await _orgUnitService.CreateOrgUnitAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Org unit created successfully"));
    }

    [HasPermission(Permissions.OrgUnit.View)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _orgUnitService.GetAllOrgUnitsAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Org units fetched successfully"));
    }
}