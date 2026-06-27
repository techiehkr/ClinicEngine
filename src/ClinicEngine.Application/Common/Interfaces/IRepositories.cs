using ClinicEngine.Domain.Entities;

namespace ClinicEngine.Application.Common.Interfaces;


public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Appointment?> GetByIdWithHistoryAsync(int id, CancellationToken cancellationToken = default);


    Task<bool> IsSlotTakenAsync(int doctorId, DateTime appointmentDateTime, int? excludeAppointmentId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
        int? patientId,
        int? doctorId,
        DateTime? from,
        DateTime? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}


public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Doctor> Items, int TotalCount)> GetPagedAsync(
        int? departmentId,
        string? specialization,
        DateOnly? availableDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}


public interface IAvailabilityRepository
{
    Task<DoctorAvailability?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorAvailability>> GetByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);

    Task<bool> HasOverlapAsync(int doctorId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(DoctorAvailability availability, CancellationToken cancellationToken = default);


    Task<DoctorAvailability?> AcquireSlotLockAsync(int doctorId, DateOnly date, TimeOnly slotTime, CancellationToken cancellationToken = default);
}


public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default);
}


public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
