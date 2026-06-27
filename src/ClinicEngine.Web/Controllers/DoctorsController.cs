using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Application.Doctors.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClinicEngine.Web.Controllers;


[ApiController]
[Route("api/doctors")]
[Produces("application/json")]
public sealed class DoctorsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDepartmentRepository _departmentRepository;

    public DoctorsController(IMediator mediator, IDepartmentRepository departmentRepository)
    {
        _mediator = mediator;
        _departmentRepository = departmentRepository;
    }


    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors(
        [FromQuery] int? departmentId,
        [FromQuery] string? specialization,
        [FromQuery] DateOnly? availableDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetDoctorsQuery(departmentId, specialization, availableDate, pageNumber, pageSize),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }


    [HttpGet("/api/departments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken = default)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        return Ok(departments.Select(d => new { d.Id, d.Name, d.Code }));
    }
}
