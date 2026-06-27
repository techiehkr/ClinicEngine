using ClinicEngine.Domain.Common;
using ClinicEngine.Domain.Exceptions;

namespace ClinicEngine.Domain.Entities;


public sealed class Patient : AuditableEntity
{
    private Patient() { }  

    public string Name { get; private set; } = default!;
    public string Contact { get; private set; } = default!;
    public string Email { get; private set; } = default!;

    
    public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();
    private readonly List<Appointment> _appointments = new();

    public static Patient Create(string name, string contact, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Patient name cannot be empty.");

        if (string.IsNullOrWhiteSpace(contact))
            throw new DomainValidationException("Contact number cannot be empty.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainValidationException("A valid email address is required.");

        return new Patient
        {
            Name = name.Trim(),
            Contact = contact.Trim(),
            Email = email.Trim().ToLowerInvariant()
        };
    }

    public void Update(string name, string contact, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Patient name cannot be empty.");

        Name = name.Trim();
        Contact = contact.Trim();
        Email = email.Trim().ToLowerInvariant();
    }
}
