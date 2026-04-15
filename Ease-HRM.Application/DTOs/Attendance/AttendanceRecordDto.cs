namespace Ease_HRM.Application.DTOs.Attendance;

public class AttendanceRecordDto
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
}