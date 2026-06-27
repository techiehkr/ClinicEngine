using ClinicEngine.Domain.Common;
using ClinicEngine.Domain.Enums;
using ClinicEngine.Domain.Exceptions;

namespace ClinicEngine.Domain.Entities;


public sealed class Appointment : AuditableEntity
{
    private Appointment() { }  

    public int PatientId { get; private set; }
    public int DoctorId { get; private set; }
    public DateTime AppointmentDateTime { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = default!;

    
    public Patient Patient { get; private set; } = default!;
    public Doctor Doctor { get; private set; } = default!;
    public IReadOnlyCollection<AppointmentHistory> History => _history.AsReadOnly();
    private readonly List<AppointmentHistory> _history = new();

    public static Appointment Create(int patientId, int doctorId, DateTime appointmentDateTime)
    {
        if (patientId <= 0)
            throw new DomainValidationException("A valid patient must be specified.");

        if (doctorId <= 0)
            throw new DomainValidationException("A valid doctor must be specified.");

        if (appointmentDateTime <= DateTime.UtcNow)
            throw new DomainValidationException("Appointment date and time must be in the future.");

        return new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDateTime = appointmentDateTime,
            Status = AppointmentStatus.Booked
        };
    }


    public void Reschedule(DateTime newDateTime, string actionBy)
    {
        if (Status != AppointmentStatus.Booked)
            throw new DomainValidationException(
                $"Only a Booked appointment can be rescheduled. Current status: {Status}.");

        if (newDateTime <= DateTime.UtcNow)
            throw new DomainValidationException("Rescheduled date and time must be in the future.");

        var previous = Status;
        AppointmentDateTime = newDateTime;
        Status = AppointmentStatus.Swapped;

        _history.Add(AppointmentHistory.Create(Id, actionBy, previous, Status));
    }

 
    public void Cancel(string actionBy)
    {
        if (Status is not (AppointmentStatus.Booked or AppointmentStatus.Swapped))
            throw new DomainValidationException(
                $"Cannot cancel an appointment with status '{Status}'.");

        var previous = Status;
        Status = AppointmentStatus.Cancelled;

        _history.Add(AppointmentHistory.Create(Id, actionBy, previous, Status));
    }


    public void Complete(string actionBy)
    {
        if (Status is not (AppointmentStatus.Booked or AppointmentStatus.Swapped))
            throw new DomainValidationException(
                $"Cannot complete an appointment with status '{Status}'.");

        var previous = Status;
        Status = AppointmentStatus.Completed;

        _history.Add(AppointmentHistory.Create(Id, actionBy, previous, Status));
    }
}
