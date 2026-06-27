using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Application.Common.Models;
using ClinicEngine.Domain.Entities;
using ClinicEngine.Domain.Exceptions;
using FluentValidation;
using MediatR;


namespace ClinicEngine.Application.Appointments.Commands;


public sealed record BookAppointmentCommand(
    int PatientId,
    int DoctorId,
    DateTime AppointmentDateTime) : IRequest<Result<int>>;



public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .GreaterThan(0).WithMessage("A valid patient ID is required.");

        RuleFor(x => x.DoctorId)
            .GreaterThan(0).WithMessage("A valid doctor ID is required.");

        RuleFor(x => x.AppointmentDateTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Appointment must be scheduled in the future.");
    }
}



public sealed class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, Result<int>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public BookAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IAvailabilityRepository availabilityRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _appointmentRepository = appointmentRepository;
        _availabilityRepository = availabilityRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result<int>.Failure($"Patient with ID {request.PatientId} was not found.");

        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<int>.Failure($"Doctor with ID {request.DoctorId} was not found.");

        var slotDate = DateOnly.FromDateTime(request.AppointmentDateTime);
        var slotTime = TimeOnly.FromDateTime(request.AppointmentDateTime);

        int newAppointmentId = 0;


        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var lockedSlot = await _availabilityRepository.AcquireSlotLockAsync(
                request.DoctorId, slotDate, slotTime, cancellationToken);

            if (lockedSlot is null)
                throw new SlotUnavailableException(
                    $"The requested time slot is not within Dr. {doctor.Name}'s availability schedule.");

           
            var taken = await _appointmentRepository.IsSlotTakenAsync(
                request.DoctorId, request.AppointmentDateTime, cancellationToken: cancellationToken);

            if (taken)
                throw new SlotUnavailableException(
                    "This slot has just been booked by another user. Please select a different time.");

            var appointment = Appointment.Create(
                request.PatientId,
                request.DoctorId,
                request.AppointmentDateTime);

            await _appointmentRepository.AddAsync(appointment, cancellationToken);


            await _unitOfWork.SaveChangesAsync(cancellationToken);

            newAppointmentId = appointment.Id;

        }, cancellationToken);

        return Result<int>.Success(newAppointmentId);
    }
}
