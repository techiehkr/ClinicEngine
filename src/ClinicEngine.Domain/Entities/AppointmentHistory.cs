using ClinicEngine.Domain.Common;
using ClinicEngine.Domain.Enums;
using ClinicEngine.Domain.Exceptions;

namespace ClinicEngine.Domain.Entities;

public sealed class AppointmentHistory : AuditableEntity
{
    private AppointmentHistory() { } 

    public int AppointmentId { get; private set; }


    public string ActionBy { get; private set; } = default!;


    public string StatusTransition { get; private set; } = default!;


    public DateTime Timestamp { get; private set; }


    public Appointment Appointment { get; private set; } = default!;

    internal static AppointmentHistory Create(
        int appointmentId,
        string actionBy,
        AppointmentStatus from,
        AppointmentStatus to)
    {
        if (string.IsNullOrWhiteSpace(actionBy))
            throw new DomainValidationException("ActionBy cannot be empty.");

        return new AppointmentHistory
        {
            AppointmentId = appointmentId,
            ActionBy = actionBy.Trim(),
            StatusTransition = $"{from} -> {to}",
            Timestamp = DateTime.UtcNow
        };
    }
}
