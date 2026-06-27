using ClinicEngine.Application;
using ClinicEngine.Infrastructure;
using ClinicEngine.Infrastructure.Persistence.Seed;
using ClinicEngine.Infrastructure.Persistence;
using ClinicEngine.Web.Middleware;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);


builder.Services.AddRazorPages();


builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });


builder.Services.AddProblemDetails();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "ClinicEngine API",
        Version = "v1",
        Description = "RESTful API for the Clinic Appointment Engine. " +
                      "All endpoints use server-side filtering and pagination."
    });
});

var app = builder.Build();


app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClinicEngine API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();


app.MapGet("/", () => Results.Redirect("/admin/dashboard"));



using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await ApplicationDbContextSeed.SeedAsync(context);
        logger.LogInformation("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();


public partial class Program { }
