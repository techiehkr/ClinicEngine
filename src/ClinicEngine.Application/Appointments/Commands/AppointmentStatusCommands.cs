using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Application.Common.Models;
using ClinicEngine.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace ClinicEngine.Application.Appointments.Commands;

public sealed record RescheduleAppointmentCommand(
    int AppointmentId,
    DateTime NewDateTime) : IRequest<Result>;

public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).GreaterThan(0);
        RuleFor(x => x.NewDateTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Rescheduled date must be in the future.");
    }
}

public sealed class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Result>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RescheduleAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IAvailabilityRepository availabilityRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _appointmentRepository = appointmentRepository;
        _availabilityRepository = availabilityRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
            return Result.Failure($"Appointment {request.AppointmentId} was not found.");

        var slotDate = DateOnly.FromDateTime(request.NewDateTime);
        var slotTime = TimeOnly.FromDateTime(request.NewDateTime);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var lockedSlot = await _availabilityRepository.AcquireSlotLockAsync(
                appointment.DoctorId, slotDate, slotTime, cancellationToken);

            if (lockedSlot is null)
                throw new SlotUnavailableException("The new time slot is outside the doctor's availability.");

            var taken = await _appointmentRepository.IsSlotTakenAsync(
                appointment.DoctorId, request.NewDateTime,
                excludeAppointmentId: appointment.Id,
                cancellationToken: cancellationToken);

            if (taken)
                throw new SlotUnavailableException("The selected time slot is already booked.");

            appointment.Reschedule(request.NewDateTime, _currentUser.UserName);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        }, cancellationToken);

        return Result.Success();
    }
}



public sealed record CancelAppointmentCommand(int AppointmentId) : IRequest<Result>;

public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).GreaterThan(0);
    }
}

public sealed class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CancelAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
            return Result.Failure($"Appointment {request.AppointmentId} was not found.");

        appointment.Cancel(_currentUser.UserName);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}



public sealed record CompleteAppointmentCommand(int AppointmentId) : IRequest<Result>;

public sealed class CompleteAppointmentCommandHandler : IRequestHandler<CompleteAppointmentCommand, Result>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CompleteAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
            return Result.Failure($"Appointment {request.AppointmentId} was not found.");

        appointment.Complete(_currentUser.UserName);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
