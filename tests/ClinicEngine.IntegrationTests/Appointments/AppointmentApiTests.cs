using ClinicEngine.Infrastructure.Persistence;
using ClinicEngine.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ClinicEngine.IntegrationTests.Appointments;


public sealed class AppointmentApiTests : IClassFixture<ClinicEngineWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ClinicEngineWebApplicationFactory _factory;

    public AppointmentApiTests(ClinicEngineWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAvailableSlots_WithValidDoctorAndDate_ShouldReturn200()
    {
        // Arrange
        int doctorId;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


            var doctor = Domain.Entities.Doctor.Create("Dr. SlotTest", "General", 1);
            db.Doctors.Add(doctor);
            await db.SaveChangesAsync();

            doctorId = doctor.Id;

            var avail = Domain.Entities.DoctorAvailability.Create(
                doctorId,
                date,
                new TimeOnly(9, 0),
                new TimeOnly(13, 0),
                30);
            db.DoctorAvailabilities.Add(avail);
            await db.SaveChangesAsync();
        }

    
        var response = await _client.GetAsync(
            $"/api/appointments/available-slots?doctorId={doctorId}&date={date:yyyy-MM-dd}");
        // Assert
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK,
            $"Expected 200 but got {(int)response.StatusCode}. Body: {body}");
    }
    [Fact]
    public async Task GetAppointments_WithPagination_ShouldReturnPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/appointments?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("pageNumber", out var pageNumber).Should().BeTrue();
        doc.RootElement.TryGetProperty("pageSize", out var pageSize).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();

        pageNumber.GetInt32().Should().Be(1);
        pageSize.GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task BookAppointment_WithInvalidPatientId_ShouldReturn400()
    {
        // Arrange
        var command = new
        {
            PatientId = 0,       
            DoctorId = 1,
            AppointmentDateTime = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/appointments", command);

  
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelAppointment_WithNonExistentId_ShouldReturn400()
    {
        // Act
        var response = await _client.PutAsync("/api/appointments/99999/cancel", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task ConcurrentBooking_ForSameSlot_ShouldAllowOnlyOneSuccess()
    {
        // Arrange 
        var (patientId, doctorId, slotDateTime) = await SeedConcurrencyTestDataAsync();

        var bookingPayload = new
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDateTime = slotDateTime
        };

        var responses = new List<HttpResponseMessage>();
        var responseLock = new object();

        // Act 
        await Parallel.ForEachAsync(Enumerable.Range(0, 2), async (_, ct) =>
        {
            var response = await _client.PostAsJsonAsync("/api/appointments", bookingPayload, ct);
            lock (responseLock) { responses.Add(response); }
        });

        // Assert 
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var failureCount = responses.Count(r =>
            r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest
                or HttpStatusCode.InternalServerError);

        (successCount + failureCount).Should().Be(2,
            "both requests must complete with a definitive response");

        failureCount.Should().BeGreaterThanOrEqualTo(1,
            "at least one concurrent booking for the same slot must be rejected");
    }

    private async Task<(int patientId, int doctorId, DateTime slotDateTime)> SeedConcurrencyTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


        var dept = db.Departments.First();

        var doctor = Domain.Entities.Doctor.Create("Dr. ConcurrencyTest", "Concurrency", dept.Id);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var patient = Domain.Entities.Patient.Create(
            "Concurrency Patient",
            "9000000001",
            $"concurrency_{Guid.NewGuid():N}@test.com");
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var slotDateTime = slotDate.ToDateTime(new TimeOnly(10, 0));

        var avail = Domain.Entities.DoctorAvailability.Create(
            doctor.Id, slotDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0), 30);
        db.DoctorAvailabilities.Add(avail);
        await db.SaveChangesAsync();

        return (patient.Id, doctor.Id, slotDateTime);
    }

   
}
    
