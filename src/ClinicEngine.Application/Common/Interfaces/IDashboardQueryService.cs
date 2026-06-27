namespace ClinicEngine.Application.Common.Interfaces;


public interface IDashboardQueryService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TodayScheduleItemDto>> GetTodayScheduleAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentBookingDto>> GetDepartmentBookingStatsAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardSummaryDto(
    int TotalActiveDoctors,
    int TodayPendingAppointments,
    int TodayCancelledSlots,
    int TotalAppointmentsToday);

public sealed record TodayScheduleItemDto(
    int AppointmentId,
    string PatientName,
    string DoctorName,
    string DepartmentName,
    DateTime AppointmentDateTime,
    string Status);

public sealed record DepartmentBookingDto(
    string DepartmentName,
    string DepartmentCode,
    int TotalBookings);
