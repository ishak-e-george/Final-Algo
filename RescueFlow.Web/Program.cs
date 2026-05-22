using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;
using RescueFlow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DbContext
builder.Services.AddDbContext<RescueFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register SC-RCMDA Algorithm Services
builder.Services.AddScoped<IRoutingService, RoutingService>();
builder.Services.AddScoped<CaseFactsBuilder>();
builder.Services.AddScoped<CaseClassifier>();
builder.Services.AddScoped<SeverityAnalyzer>();
builder.Services.AddScoped<RequirementEngine>();
builder.Services.AddScoped<ResourceMatcher>();
builder.Services.AddScoped<HospitalMatcher>();
builder.Services.AddScoped<ResponsePlanGenerator>();
builder.Services.AddScoped<SafetyValidator>();
builder.Services.AddScoped<AutoAssignmentService>();
builder.Services.AddScoped<AuditLogger>();

var app = builder.Build();

// Seed database on startup if configured
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RescueFlowDbContext>();
        // Automatically run migrations
        context.Database.Migrate();
        // Seed data
        SeedData.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
