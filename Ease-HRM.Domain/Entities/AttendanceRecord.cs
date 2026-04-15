using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Domain.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalHours { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}