using ClinicEngine.Domain.Common;
using ClinicEngine.Domain.Exceptions;

namespace ClinicEngine.Domain.Entities;


public sealed class Doctor : AuditableEntity
{
    private Doctor() { }  

    public string Name { get; private set; } = default!;
    public string Specialization { get; private set; } = default!;

    public int DepartmentId { get; private set; }


    public Department Department { get; private set; } = default!;
    public IReadOnlyCollection<DoctorAvailability> Availabilities => _availabilities.AsReadOnly();
    public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

    private readonly List<DoctorAvailability> _availabilities = new();
    private readonly List<Appointment> _appointments = new();

    public static Doctor Create(string name, string specialization, int departmentId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Doctor name cannot be empty.");

        if (string.IsNullOrWhiteSpace(specialization))
            throw new DomainValidationException("Specialization cannot be empty.");

        if (departmentId <= 0)
            throw new DomainValidationException("A valid department must be assigned.");

        return new Doctor
        {
            Name = name.Trim(),
            Specialization = specialization.Trim(),
            DepartmentId = departmentId
        };
    }

    public void Update(string name, string specialization, int departmentId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Doctor name cannot be empty.");

        Name = name.Trim();
        Specialization = specialization.Trim();
        DepartmentId = departmentId;
    }
}
