using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Helpers;

public static class AttendanceCalculator
{
    public static decimal CalculateTotalHours(IEnumerable<AttendanceSession> sessions)
    {
        decimal total = 0;

        foreach (var session in sessions)
        {
            var sessionEnd = session.CheckOutTime ?? DateTime.UtcNow;

            if (sessionEnd.Date > session.CheckInTime.Date)
            {
                sessionEnd = session.CheckInTime.Date.AddDays(1);
            }

            var duration = sessionEnd - session.CheckInTime;

            if (duration.TotalSeconds <= 0)
            {
                continue;
            }

            total += (decimal)duration.TotalHours;
        }

        return total;
    }
}
