namespace Ease_HRM.Domain.Entities;

public class AttendanceSession
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
}