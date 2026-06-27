using ClinicEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicEngine.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(150);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(10);
        builder.HasIndex(d => d.Code).IsUnique();
        builder.Property(d => d.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}

internal sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Specialization).IsRequired().HasMaxLength(200);
        builder.Property(d => d.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);

        builder.HasOne(d => d.Department)
            .WithMany(dep => dep.Doctors)
            .HasForeignKey(d => d.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.DepartmentId);
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Contact).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(200);
        builder.Property(p => p.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

internal sealed class DoctorAvailabilityConfiguration : IEntityTypeConfiguration<DoctorAvailability>
{
    public void Configure(EntityTypeBuilder<DoctorAvailability> builder)
    {
        builder.ToTable("DoctorAvailabilities");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.SlotDurationMinutes).IsRequired();
        builder.Property(a => a.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.HasOne(a => a.Doctor)
            .WithMany(d => d.Availabilities)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasIndex(a => new { a.DoctorId, a.AvailableDate });
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);


        builder.Property(a => a.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .HasDefaultValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasIndex(a => new { a.DoctorId, a.AppointmentDateTime })
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled'");

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

internal sealed class AppointmentHistoryConfiguration : IEntityTypeConfiguration<AppointmentHistory>
{
    public void Configure(EntityTypeBuilder<AppointmentHistory> builder)
    {
        builder.ToTable("AppointmentHistories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.ActionBy).IsRequired().HasMaxLength(100);
        builder.Property(h => h.StatusTransition).IsRequired().HasMaxLength(50);
        builder.Property(h => h.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(h => h.UpdatedBy).HasMaxLength(100);

        builder.HasOne(h => h.Appointment)
            .WithMany(a => a.History)
            .HasForeignKey(h => h.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.AppointmentId);


        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}