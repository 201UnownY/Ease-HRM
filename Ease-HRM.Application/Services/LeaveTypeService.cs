using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.LeaveTypes;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class LeaveTypeService : ILeaveTypeService
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly IExceptionTranslator _exceptionTranslator;

    public LeaveTypeService(ILeaveTypeRepository leaveTypeRepository, IExceptionTranslator exceptionTranslator)
    {
        _leaveTypeRepository = leaveTypeRepository;
        _exceptionTranslator = exceptionTranslator;
    }

    public async Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedName = StringHelper.Normalize(request.Name, "Leave type name");

        if (request.DefaultDays <= 0)
        {
            throw new ArgumentException("DefaultDays must be greater than 0.");
        }

        if (request.Weight <= 0)
        {
            throw new ArgumentException("Weight must be greater than 0.");
        }

        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            DefaultDays = request.DefaultDays,
            Weight = request.Weight,
            IsPaid = request.IsPaid
        };

        try
        {
            await _leaveTypeRepository.AddAsync(leaveType, cancellationToken);
            await _leaveTypeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (_exceptionTranslator.IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("Duplicate record detected.");
        }

        return new LeaveTypeDto
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            DefaultDays = leaveType.DefaultDays,
            Weight = leaveType.Weight,
            IsPaid = leaveType.IsPaid
        };
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetAllLeaveTypesAsync(CancellationToken cancellationToken = default)
    {
        var leaveTypes = await _leaveTypeRepository.GetAllAsync(cancellationToken);

        return leaveTypes
            .Select(x => new LeaveTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                DefaultDays = x.DefaultDays,
                Weight = x.Weight,
                IsPaid = x.IsPaid
            })
            .ToList()
            .AsReadOnly();
    }
}