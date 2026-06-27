using ClinicEngine.Application.Appointments.Commands;
using ClinicEngine.Application.Appointments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClinicEngine.Web.Controllers;


[ApiController]
[Route("api/appointments")]
[Produces("application/json")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] int? patientId,
        [FromQuery] int? doctorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAppointmentsQuery(patientId, doctorId, from, to, pageNumber, pageSize),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("available-slots")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int doctorId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAvailableSlotsQuery(doctorId, date),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }


    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookAppointment(
        [FromBody] BookAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(
            nameof(GetAvailableSlots),
            new { id = result.Value },
            new { appointmentId = result.Value, message = "Appointment booked successfully." });
    }


    [HttpPut("{id:int}/reschedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RescheduleAppointment(
        int id,
        [FromBody] RescheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RescheduleAppointmentCommand(id, request.NewDateTime),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPut("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelAppointment(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CancelAppointmentCommand(id), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }


    [HttpPut("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteAppointment(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CompleteAppointmentCommand(id), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }
}


public sealed record RescheduleRequest(DateTime NewDateTime);
