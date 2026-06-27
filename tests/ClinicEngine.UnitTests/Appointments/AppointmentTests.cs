using ClinicEngine.Application.Appointments.Commands;
using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Domain.Entities;
using ClinicEngine.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace ClinicEngine.UnitTests.Appointments;


public sealed class AppointmentEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnBookedAppointment()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act
        var appointment = Appointment.Create(patientId: 1, doctorId: 1, appointmentDateTime: futureDate);

        // Assert
        appointment.PatientId.Should().Be(1);
        appointment.DoctorId.Should().Be(1);
        appointment.Status.Should().Be(Domain.Enums.AppointmentStatus.Booked);
    }

    [Fact]
    public void Create_WithPastDateTime_ShouldThrowDomainValidationException()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var act = () => Appointment.Create(1, 1, pastDate);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*future*");
    }

    [Fact]
    public void Cancel_WhenBooked_ShouldTransitionToCancelled()
    {
        // Arrange
        var appointment = Appointment.Create(1, 1, DateTime.UtcNow.AddDays(1));

        // Act
        appointment.Cancel("test-user");

        // Assert
        appointment.Status.Should().Be(Domain.Enums.AppointmentStatus.Cancelled);
        appointment.History.Should().HaveCount(1);
        appointment.History.First().StatusTransition.Should().Contain("Cancelled");
        appointment.History.First().ActionBy.Should().Be("test-user");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowDomainValidationException()
    {
        // Arrange
        var appointment = Appointment.Create(1, 1, DateTime.UtcNow.AddDays(1));
        appointment.Cancel("user1");

        // Act
        var act = () => appointment.Cancel("user2");

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Reschedule_WhenBooked_ShouldTransitionToSwapped()
    {
        // Arrange
        var appointment = Appointment.Create(1, 1, DateTime.UtcNow.AddDays(1));
        var newDate = DateTime.UtcNow.AddDays(2);

        // Act
        appointment.Reschedule(newDate, "admin");

        // Assert
        appointment.Status.Should().Be(Domain.Enums.AppointmentStatus.Swapped);
        appointment.AppointmentDateTime.Should().Be(newDate);
        appointment.History.Should().HaveCount(1);
    }

    [Fact]
    public void Reschedule_WhenCancelled_ShouldThrowDomainValidationException()
    {
        // Arrange
        var appointment = Appointment.Create(1, 1, DateTime.UtcNow.AddDays(1));
        appointment.Cancel("user");

        // Act
        var act = () => appointment.Reschedule(DateTime.UtcNow.AddDays(2), "admin");

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Complete_WhenBooked_ShouldTransitionToCompleted()
    {
        // Arrange
        var appointment = Appointment.Create(1, 1, DateTime.UtcNow.AddDays(1));

        // Act
        appointment.Complete("admin");

        // Assert
        appointment.Status.Should().Be(Domain.Enums.AppointmentStatus.Completed);
        appointment.History.Should().HaveCount(1);
    }
}


public sealed class DoctorAvailabilityEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Act
        var availability = DoctorAvailability.Create(
            doctorId: 1,
            availableDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(13, 0),
            slotDurationMinutes: 30);

        // Assert
        availability.Should().NotBeNull();
        availability.SlotDurationMinutes.Should().Be(30);
    }

    [Fact]
    public void Create_WithStartTimeAfterEndTime_ShouldThrow()
    {
        // Act
        var act = () => DoctorAvailability.Create(
            1,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            new TimeOnly(14, 0),
            new TimeOnly(9, 0),
            30);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*Start time must be before end time*");
    }

    [Fact]
    public void Create_WhenSlotDurationDoesNotDivideWindow_ShouldThrow()
    {
        // 9:00 to 12:00 = 180 minutes. 180 % 25 != 0.
        var act = () => DoctorAvailability.Create(
            1,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            slotDurationMinutes: 25);

        act.Should().Throw<DomainValidationException>()
            .WithMessage("*evenly divisible*");
    }

    [Fact]
    public void GetSlotTimes_ShouldReturnCorrectNumberOfSlots()
    {
        // 9:00 to 11:00 = 120 minutes / 30 = 4 slots
        var availability = DoctorAvailability.Create(
            1,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            30);

        var slots = availability.GetSlotTimes().ToList();

        slots.Should().HaveCount(4);
        slots[0].Should().Be(new TimeOnly(9, 0));
        slots[1].Should().Be(new TimeOnly(9, 30));
        slots[2].Should().Be(new TimeOnly(10, 0));
        slots[3].Should().Be(new TimeOnly(10, 30));
    }

    [Fact]
    public void OverlapsWith_WhenWindowsOverlap_ShouldReturnTrue()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var a = DoctorAvailability.Create(1, date, new TimeOnly(9, 0), new TimeOnly(12, 0), 30);
        var b = DoctorAvailability.Create(1, date, new TimeOnly(11, 0), new TimeOnly(14, 0), 30);

        a.OverlapsWith(b).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WhenWindowsAreAdjacent_ShouldReturnFalse()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var a = DoctorAvailability.Create(1, date, new TimeOnly(9, 0), new TimeOnly(12, 0), 30);
        var b = DoctorAvailability.Create(1, date, new TimeOnly(12, 0), new TimeOnly(15, 0), 30);

        a.OverlapsWith(b).Should().BeFalse();
    }
}


public sealed class BookAppointmentCommandHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepo = new();
    private readonly Mock<IAvailabilityRepository> _availabilityRepo = new();
    private readonly Mock<IPatientRepository> _patientRepo = new();
    private readonly Mock<IDoctorRepository> _doctorRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private BookAppointmentCommandHandler CreateHandler() =>
        new(_appointmentRepo.Object, _availabilityRepo.Object,
            _patientRepo.Object, _doctorRepo.Object,
            _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_WhenPatientNotFound_ShouldReturnFailure()
    {
        // Arrange
        _patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var handler = CreateHandler();
        var command = new BookAppointmentCommand(99, 1, DateTime.UtcNow.AddDays(1));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Patient");
    }

    [Fact]
    public async Task Handle_WhenDoctorNotFound_ShouldReturnFailure()
    {
        // Arrange
        _patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Patient.Create("Test", "1234567890", "test@example.com"));

        _doctorRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        var handler = CreateHandler();
        var command = new BookAppointmentCommand(1, 99, DateTime.UtcNow.AddDays(1));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Doctor");
    }

    [Fact]
    public async Task Handle_WhenSlotLockReturnsNull_ShouldThrowSlotUnavailableException()
    {
        // Arrange
        _patientRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Patient.Create("Test Patient", "9876543210", "p@example.com"));

        _doctorRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Doctor.Create("Dr. Test", "Cardiology", 1));
        
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());


        _availabilityRepo.Setup(r => r.AcquireSlotLockAsync(
                It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorAvailability?)null);

        var handler = CreateHandler();
        var command = new BookAppointmentCommand(1, 1, DateTime.UtcNow.AddDays(1));

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SlotUnavailableException>();
    }
}
