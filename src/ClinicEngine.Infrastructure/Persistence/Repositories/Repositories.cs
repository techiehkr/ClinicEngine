using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicEngine.Infrastructure.Persistence.Repositories;



public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly ApplicationDbContext _context;

    public AppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Appointment?> GetByIdWithHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> IsSlotTakenAsync(
        int doctorId,
        DateTime appointmentDateTime,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Where(a => a.DoctorId == doctorId
                     && a.AppointmentDateTime == appointmentDateTime
                     && a.Status != Domain.Enums.AppointmentStatus.Cancelled
                     && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
            .AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDoctorAndDateAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = date.ToDateTime(TimeOnly.MaxValue);

        return await _context.Appointments
            .Where(a => a.DoctorId == doctorId
                     && a.AppointmentDateTime >= start
                     && a.AppointmentDateTime <= end
                     && a.Status != Domain.Enums.AppointmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _context.Appointments.AddAsync(appointment, cancellationToken);
    }

    public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
        int? patientId,
        int? doctorId,
        DateTime? from,
        DateTime? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .AsQueryable();

        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId.Value);
        if (doctorId.HasValue)  query = query.Where(a => a.DoctorId == doctorId.Value);
        if (from.HasValue)      query = query.Where(a => a.AppointmentDateTime >= from.Value);
        if (to.HasValue)        query = query.Where(a => a.AppointmentDateTime <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.AppointmentDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}



public sealed class DoctorRepository : IDoctorRepository
{
    private readonly ApplicationDbContext _context;

    public DoctorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Doctor> Items, int TotalCount)> GetPagedAsync(
        int? departmentId,
        string? specialization,
        DateOnly? availableDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Doctors
            .Include(d => d.Department)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(d => d.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(specialization))
            query = query.Where(d => d.Specialization.Contains(specialization));

        if (availableDate.HasValue)
            query = query.Where(d =>
                d.Availabilities.Any(a => a.AvailableDate == availableDate.Value));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(d => d.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}



public sealed class AvailabilityRepository : IAvailabilityRepository
{
    private readonly ApplicationDbContext _context;

    public AvailabilityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DoctorAvailability?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DoctorAvailabilities
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorAvailability>> GetByDoctorAndDateAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && a.AvailableDate == date)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlapAsync(
        int doctorId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId
                     && a.AvailableDate == date
                     && (excludeId == null || a.Id != excludeId)
                     && a.StartTime < end
                     && a.EndTime > start)
            .AnyAsync(cancellationToken);
    }

    public async Task AddAsync(DoctorAvailability availability, CancellationToken cancellationToken = default)
    {
        await _context.DoctorAvailabilities.AddAsync(availability, cancellationToken);
    }


    public async Task<DoctorAvailability?> AcquireSlotLockAsync(
        int doctorId,
        DateOnly date,
        TimeOnly slotTime,
        CancellationToken cancellationToken = default)
    {

        if (_context.Database.IsRelational())
        {
            return await _context.DoctorAvailabilities
                .FromSqlRaw(
                    @"SELECT TOP 1 * FROM DoctorAvailabilities WITH (UPDLOCK, ROWLOCK)
                  WHERE DoctorId = {0}
                    AND AvailableDate = {1}
                    AND StartTime <= {2}
                    AND EndTime > {2}
                    AND IsDeleted = 0",
                    doctorId, date, slotTime)
                .FirstOrDefaultAsync(cancellationToken);
        }


        return await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId
                     && a.AvailableDate == date
                     && a.StartTime <= slotTime
                     && a.EndTime > slotTime)
            .FirstOrDefaultAsync(cancellationToken);
    }
}



public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _context;

    public DepartmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Domain.Entities.Department>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments.OrderBy(d => d.Name).ToListAsync(cancellationToken);
    }
}


public sealed class PatientRepository : IPatientRepository
{
    private readonly ApplicationDbContext _context;

    public PatientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
    public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Patients
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
