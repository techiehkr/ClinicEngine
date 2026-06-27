using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicEngine.Infrastructure.Persistence;


public sealed class DashboardQueryService : IDashboardQueryService
{
    private readonly ApplicationDbContext _context;

    public DashboardQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

        var activeDoctors = await _context.Doctors.CountAsync(cancellationToken);

    
        var todayStatuses = await _context.Appointments
            .Where(a => a.AppointmentDateTime >= todayStart && a.AppointmentDateTime <= todayEnd)
            .Select(a => a.Status)
            .ToListAsync(cancellationToken);

        var total = todayStatuses.Count;
        var pending = todayStatuses.Count(s => s == AppointmentStatus.Booked || s == AppointmentStatus.Swapped);
        var cancelled = todayStatuses.Count(s => s == AppointmentStatus.Cancelled);

        return new DashboardSummaryDto(
            TotalActiveDoctors: activeDoctors,
            TodayPendingAppointments: pending,
            TodayCancelledSlots: cancelled,
            TotalAppointmentsToday: total);
    }
    public async Task<IReadOnlyList<TodayScheduleItemDto>> GetTodayScheduleAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

        return await _context.Appointments
            .Where(a => a.AppointmentDateTime >= todayStart && a.AppointmentDateTime <= todayEnd)
            .OrderBy(a => a.AppointmentDateTime)
            .Select(a => new TodayScheduleItemDto(
                a.Id,
                a.Patient.Name,
                a.Doctor.Name,
                a.Doctor.Department.Name,
                a.AppointmentDateTime,
                a.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DepartmentBookingDto>> GetDepartmentBookingStatsAsync(CancellationToken cancellationToken = default)
    {
     
        var raw = await (
            from a in _context.Appointments
            join d in _context.Doctors on a.DoctorId equals d.Id
            join dep in _context.Departments on d.DepartmentId equals dep.Id
            where a.Status != AppointmentStatus.Cancelled
            select new { dep.Name, dep.Code }
        ).ToListAsync(cancellationToken);

        var result = raw
            .GroupBy(x => new { x.Name, x.Code })
            .Select(g => new DepartmentBookingDto(g.Key.Name, g.Key.Code, g.Count()))
            .OrderByDescending(x => x.TotalBookings)
            .ToList();

        return result;
    }
}
