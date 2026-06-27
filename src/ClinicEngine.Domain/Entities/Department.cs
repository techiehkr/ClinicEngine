using ClinicEngine.Domain.Common;
using ClinicEngine.Domain.Exceptions;

namespace ClinicEngine.Domain.Entities;

public sealed class Department : AuditableEntity
{
    private Department() { }  

    public string Name { get; private set; } = default!;


    public string Code { get; private set; } = default!;

   
    public IReadOnlyCollection<Doctor> Doctors => _doctors.AsReadOnly();
    private readonly List<Doctor> _doctors = new();

    public static Department Create(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Department name cannot be empty.");

        if (string.IsNullOrWhiteSpace(code))
            throw new DomainValidationException("Department code cannot be empty.");

        return new Department
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant()
        };
    }

    public void Update(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Department name cannot be empty.");

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
    }
}
