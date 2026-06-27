using ClinicEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicEngine.Infrastructure.Persistence.Seed;


public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();

        if (await context.Departments.AnyAsync()) return;  



        var departments = new[]
        {
            Department.Create("Cardiology",      "CARD"),
            Department.Create("Orthopaedics",    "ORTH"),
            Department.Create("Neurology",        "NEUR"),
            Department.Create("Paediatrics",     "PAED"),
            Department.Create("General Medicine","GENM"),
        };

        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();



        var doctors = new[]
        {
            Doctor.Create("Dr. Arjun Mehta",     "Interventional Cardiology",  departments[0].Id),
            Doctor.Create("Dr. Priya Nair",      "Echocardiography",           departments[0].Id),
            Doctor.Create("Dr. Suresh Kumar",    "Spine Surgery",              departments[1].Id),
            Doctor.Create("Dr. Kavitha Reddy",   "Joint Replacement",          departments[1].Id),
            Doctor.Create("Dr. Ramesh Iyengar",  "Stroke & Epilepsy",          departments[2].Id),
            Doctor.Create("Dr. Anitha Joseph",   "Paediatric Neurology",       departments[2].Id),
            Doctor.Create("Dr. Vikram Singh",    "Neonatology",                departments[3].Id),
            Doctor.Create("Dr. Deepa Krishnan",  "Child Development",          departments[3].Id),
            Doctor.Create("Dr. Mohan Das",       "Internal Medicine",          departments[4].Id),
            Doctor.Create("Dr. Shobha Rajan",    "Diabetology",                departments[4].Id),
        };

        await context.Doctors.AddRangeAsync(doctors);
        await context.SaveChangesAsync();



        var patients = new[]
        {
            Patient.Create("Sudarshan K",     "9876543210", "sudarshan@example.com"),
            Patient.Create("Ravi Shankar",    "9876543211", "ravi.shankar@example.com"),
            Patient.Create("Lakshmi Devi",    "9876543212", "lakshmi.devi@example.com"),
            Patient.Create("Murugan P",       "9876543213", "murugan.p@example.com"),
            Patient.Create("Ananya Sharma",   "9876543214", "ananya.sharma@example.com"),
            Patient.Create("Karthik Raj",     "9876543215", "karthik.raj@example.com"),
            Patient.Create("Pooja Menon",     "9876543216", "pooja.menon@example.com"),
            Patient.Create("Arun Balaji",     "9876543217", "arun.balaji@example.com"),
            Patient.Create("Meena Iyer",      "9876543218", "meena.iyer@example.com"),
            Patient.Create("Ganesh Babu",     "9876543219", "ganesh.babu@example.com"),
            Patient.Create("Divya Prakash",   "9876543220", "divya.prakash@example.com"),
            Patient.Create("Senthil Kumar",   "9876543221", "senthil.kumar@example.com"),
            Patient.Create("Padma Venkat",    "9876543222", "padma.venkat@example.com"),
            Patient.Create("Raja Gopalan",    "9876543223", "raja.gopalan@example.com"),
            Patient.Create("Usha Nair",       "9876543224", "usha.nair@example.com"),
            Patient.Create("Dinesh Srinivas", "9876543225", "dinesh.srinivas@example.com"),
            Patient.Create("Saranya Pillai",  "9876543226", "saranya.pillai@example.com"),
            Patient.Create("Bala Murugan",    "9876543227", "bala.murugan@example.com"),
            Patient.Create("Nithya Ramesh",   "9876543228", "nithya.ramesh@example.com"),
            Patient.Create("Vijay Kumar",     "9876543229", "vijay.kumar@example.com"),
        };

        await context.Patients.AddRangeAsync(patients);
        await context.SaveChangesAsync();



        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);
        var dayAfter = today.AddDays(2);

        var availabilities = new List<DoctorAvailability>();

        foreach (var doctor in doctors)
        {
            availabilities.Add(DoctorAvailability.Create(
                doctor.Id, today, new TimeOnly(9, 0), new TimeOnly(13, 0), 30));

            availabilities.Add(DoctorAvailability.Create(
                doctor.Id, tomorrow, new TimeOnly(10, 0), new TimeOnly(14, 0), 30));

            availabilities.Add(DoctorAvailability.Create(
                doctor.Id, dayAfter, new TimeOnly(9, 0), new TimeOnly(12, 0), 15));
        }

        await context.DoctorAvailabilities.AddRangeAsync(availabilities);
        await context.SaveChangesAsync();


        if (context.Database.IsRelational())
        {
            var baseDate = today.AddDays(1).ToDateTime(new TimeOnly(9, 0));

            var appointments = new[]
            {
        Appointment.Create(patients[0].Id,  doctors[0].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[1].Id,  doctors[0].Id,  baseDate.AddMinutes(30)),
        Appointment.Create(patients[2].Id,  doctors[1].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[3].Id,  doctors[2].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[4].Id,  doctors[3].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[5].Id,  doctors[4].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[6].Id,  doctors[5].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[7].Id,  doctors[6].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[8].Id,  doctors[7].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[9].Id,  doctors[8].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[10].Id, doctors[9].Id,  baseDate.AddMinutes(0)),
        Appointment.Create(patients[11].Id, doctors[0].Id,  baseDate.AddMinutes(60)),
        Appointment.Create(patients[12].Id, doctors[1].Id,  baseDate.AddMinutes(30)),
        Appointment.Create(patients[13].Id, doctors[2].Id,  baseDate.AddMinutes(30)),
        Appointment.Create(patients[14].Id, doctors[3].Id,  baseDate.AddMinutes(30)),
    };

            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();

            appointments[13].Cancel("system");
            appointments[14].Cancel("system");
            await context.SaveChangesAsync();
        }
    }
}
