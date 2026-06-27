using ClinicEngine.Domain.Common;
using ClinicEngine.Domain.Exceptions;

namespace ClinicEngine.Domain.Entities;


public sealed class DoctorAvailability : AuditableEntity
{
    private DoctorAvailability() { }  

    public int DoctorId { get; private set; }
    public DateOnly AvailableDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    public int SlotDurationMinutes { get; private set; }

    public Doctor Doctor { get; private set; } = default!;


    public static DoctorAvailability Create(
        int doctorId,
        DateOnly availableDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        if (doctorId <= 0)
            throw new DomainValidationException("A valid doctor must be specified.");

        if (availableDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new DomainValidationException("Availability date cannot be in the past.");

        if (startTime >= endTime)
            throw new DomainValidationException("Start time must be before end time.");

        if (slotDurationMinutes <= 0)
            throw new DomainValidationException("Slot duration must be a positive number of minutes.");

        var totalMinutes = (endTime - startTime).TotalMinutes;
        if (totalMinutes % slotDurationMinutes != 0)
            throw new DomainValidationException(
                $"The window duration ({totalMinutes} min) must be evenly divisible by the slot duration ({slotDurationMinutes} min).");

        return new DoctorAvailability
        {
            DoctorId = doctorId,
            AvailableDate = availableDate,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = slotDurationMinutes
        };
    }

    public bool OverlapsWith(DoctorAvailability other)
    {
        if (other.DoctorId != DoctorId || other.AvailableDate != AvailableDate)
            return false;

        return StartTime < other.EndTime && EndTime > other.StartTime;
    }


    public IEnumerable<TimeOnly> GetSlotTimes()
    {
        var current = StartTime;
        while (current < EndTime)
        {
            yield return current;
            current = current.AddMinutes(SlotDurationMinutes);
        }
    }
}
