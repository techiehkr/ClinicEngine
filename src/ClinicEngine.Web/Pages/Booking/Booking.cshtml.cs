using ClinicEngine.Application.Appointments.Commands;
using ClinicEngine.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClinicEngine.Web.Pages.Booking;

public sealed class BookingModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPatientRepository _patientRepository;

    public BookingModel(
        IMediator mediator,
        IDoctorRepository doctorRepository,
        IDepartmentRepository departmentRepository,
        IPatientRepository patientRepository)
    {
        _mediator = mediator;
        _doctorRepository = doctorRepository;
        _departmentRepository = departmentRepository;
        _patientRepository = patientRepository;
    }

    [BindProperty]
    public BookingInputModel Input { get; set; } = new();

    public IReadOnlyList<SelectListItem> Departments { get; private set; } = [];
    public IReadOnlyList<SelectListItem> Doctors { get; private set; } = [];
    public IReadOnlyList<SelectListItem> Patients { get; private set; } = [];
    public string? SuccessMessage { get; private set; }
    public int? CreatedAppointmentId { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await PopulateDropdownsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(cancellationToken);
            return Page();
        }

        var appointmentDateTime = Input.AppointmentDate.ToDateTime(Input.AppointmentTime);

     
            if (appointmentDateTime < DateTime.UtcNow.Date)
            {
            ModelState.AddModelError(nameof(Input.AppointmentDate),
                "Appointment date and time must be in the future.");
            await PopulateDropdownsAsync(cancellationToken);
            return Page();
        }

        var command = new BookAppointmentCommand(
            Input.PatientId,
            Input.DoctorId,
            appointmentDateTime);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await PopulateDropdownsAsync(cancellationToken);
            return Page();
        }

        CreatedAppointmentId = result.Value;
        SuccessMessage = $"Appointment booked successfully. Reference ID: #{result.Value}";
        Input = new BookingInputModel();
        await PopulateDropdownsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetDoctorsByDepartmentAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        var (doctors, _) = await _doctorRepository.GetPagedAsync(
            departmentId, null, null, 1, 50, cancellationToken);

        var items = doctors.Select(d => new { d.Id, d.Name, d.Specialization });
        return new JsonResult(items);
    }

    private async Task PopulateDropdownsAsync(CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        Departments = departments
            .Select(d => new SelectListItem(d.Name, d.Id.ToString()))
            .ToList();

        var (doctors, _) = await _doctorRepository.GetPagedAsync(
            null, null, null, 1, 100, cancellationToken);
        Doctors = doctors
            .Select(d => new SelectListItem($"{d.Name} — {d.Specialization}", d.Id.ToString()))
            .ToList();

        var patients = await _patientRepository.GetAllAsync(cancellationToken);
        Patients = patients
            .Select(p => new SelectListItem($"{p.Name} ({p.Contact})", p.Id.ToString()))
            .ToList();
    }
}

public sealed class BookingInputModel
{
    [Required(ErrorMessage = "Please select a department.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid department.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Please select a doctor.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid doctor.")]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "Please select a patient.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient.")]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Please select an appointment date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Appointment Date")]
    public DateOnly AppointmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Please select an appointment time.")]
    [DataType(DataType.Time)]
    [Display(Name = "Appointment Time")]
    public TimeOnly AppointmentTime { get; set; } = new TimeOnly(9, 0);

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}