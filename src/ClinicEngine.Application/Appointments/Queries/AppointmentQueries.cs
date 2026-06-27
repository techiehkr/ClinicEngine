using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Application.Common.Models;
using ClinicEngine.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicEngine.Application.Appointments.Queries;



public sealed record AvailableSlotDto(
    int DoctorId,
    string DoctorName,
    DateOnly Date,
    TimeOnly SlotTime,
    bool IsAvailable);

public sealed record AppointmentDto(
    int Id,
    string PatientName,
    string DoctorName,
    string DepartmentName,
    DateTime AppointmentDateTime,
    string Status);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}



public sealed record GetAvailableSlotsQuery(int DoctorId, DateOnly Date) : IRequest<Result<IReadOnlyList<AvailableSlotDto>>>;

public sealed class GetAvailableSlotsQueryValidator : AbstractValidator<GetAvailableSlotsQuery>
{
    public GetAvailableSlotsQueryValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage("A valid doctor ID is required.");
    }
}

public sealed class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, Result<IReadOnlyList<AvailableSlotDto>>>
{
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;

    public GetAvailableSlotsQueryHandler(
        IAvailabilityRepository availabilityRepository,
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository)
    {
        _availabilityRepository = availabilityRepository;
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
    }

    public async Task<Result<IReadOnlyList<AvailableSlotDto>>> Handle(
        GetAvailableSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<IReadOnlyList<AvailableSlotDto>>.Failure($"Doctor {request.DoctorId} not found.");

        var availabilities = await _availabilityRepository.GetByDoctorAndDateAsync(
            request.DoctorId, request.Date, cancellationToken);

        var bookedAppointments = await _appointmentRepository.GetByDoctorAndDateAsync(
            request.DoctorId, request.Date, cancellationToken);

        var bookedTimes = bookedAppointments
            .Select(a => TimeOnly.FromDateTime(a.AppointmentDateTime))
            .ToHashSet();

        var slots = availabilities
            .SelectMany(a => a.GetSlotTimes())
            .Select(slotTime => new AvailableSlotDto(
                request.DoctorId,
                doctor.Name,
                request.Date,
                slotTime,
                IsAvailable: !bookedTimes.Contains(slotTime)))
            .OrderBy(s => s.SlotTime)
            .ToList();

        return Result<IReadOnlyList<AvailableSlotDto>>.Success(slots);
    }
}



public sealed record GetAppointmentsQuery(
    int? PatientId,
    int? DoctorId,
    DateTime? From,
    DateTime? To,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<AppointmentDto>>>;

public sealed class GetAppointmentsQueryHandler
    : IRequestHandler<GetAppointmentsQuery, Result<PagedResult<AppointmentDto>>>
{
    private readonly IAppointmentRepository _appointmentRepository;

    public GetAppointmentsQueryHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<Result<PagedResult<AppointmentDto>>> Handle(
        GetAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _appointmentRepository.GetPagedAsync(
            request.PatientId, request.DoctorId, request.From, request.To,
            request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(a => new AppointmentDto(
            a.Id,
            a.Patient.Name,
            a.Doctor.Name,
            a.Doctor.Department.Name,
            a.AppointmentDateTime,
            a.Status.ToString())).ToList();

        return Result<PagedResult<AppointmentDto>>.Success(
            new PagedResult<AppointmentDto>(dtos, totalCount, request.PageNumber, request.PageSize));
    }
}
