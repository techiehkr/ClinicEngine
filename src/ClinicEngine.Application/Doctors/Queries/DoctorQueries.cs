using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Application.Common.Models;
using ClinicEngine.Application.Appointments.Queries;
using FluentValidation;
using MediatR;

namespace ClinicEngine.Application.Doctors.Queries;

public sealed record DoctorDto(
    int Id,
    string Name,
    string Specialization,
    int DepartmentId,
    string DepartmentName);

public sealed record GetDoctorsQuery(
    int? DepartmentId,
    string? Specialization,
    DateOnly? AvailableDate,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<DoctorDto>>>;

public sealed class GetDoctorsQueryHandler
    : IRequestHandler<GetDoctorsQuery, Result<PagedResult<DoctorDto>>>
{
    private readonly IDoctorRepository _doctorRepository;

    public GetDoctorsQueryHandler(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;
    }

    public async Task<Result<PagedResult<DoctorDto>>> Handle(
        GetDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _doctorRepository.GetPagedAsync(
            request.DepartmentId,
            request.Specialization,
            request.AvailableDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(d => new DoctorDto(
            d.Id,
            d.Name,
            d.Specialization,
            d.DepartmentId,
            d.Department.Name)).ToList();

        return Result<PagedResult<DoctorDto>>.Success(
            new PagedResult<DoctorDto>(dtos, totalCount, request.PageNumber, request.PageSize));
    }
}
