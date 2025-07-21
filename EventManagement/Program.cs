using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EventManagement.Models;
using EventManagement.Services;
using EventManagement.Hubs;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add DbContext
builder.Services.AddDbContext<EventManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<NotificationService>();

// Add SignalR
builder.Services.AddSignalR();

// Add Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EventManagementDbContext>();
    await SeedDatabase(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add session middleware
app.UseSession();

// Add authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add SignalR endpoint
app.MapHub<NotificationHub>("/notificationHub");

// Add Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapRazorPages();

// Schedule recurring jobs
RecurringJob.AddOrUpdate<EventService>(
    "check-event-reminders",
    x => x.SendEventRemindersAsync(),
    Cron.Daily(9, 0)); // Run at 9:00 AM every day

app.Run();

async Task SeedDatabase(EventManagementDbContext context)
{
    // Seed Roles
    if (!await context.Roles.AnyAsync())
    {
        var roles = new[]
        {
            new Role { RoleName = "Admin", Description = "System Administrator with full access", CreatedAt = DateTime.Now, IsActive = true },
            new Role { RoleName = "Organizer", Description = "Event organizer who can create and manage events", CreatedAt = DateTime.Now, IsActive = true },
            new Role { RoleName = "Staff", Description = "Event staff member with limited management access", CreatedAt = DateTime.Now, IsActive = true },
            new Role { RoleName = "Attendee", Description = "Regular user who can attend events", CreatedAt = DateTime.Now, IsActive = true }
        };
        await context.Roles.AddRangeAsync(roles);
    }

    // Seed Event Types
    if (!await context.EventTypes.AnyAsync())
    {
        var eventTypes = new[]
        {
            new EventType { EventTypeName = "Conference", Description = "Professional conferences and seminars", IconClass = "fas fa-users", ColorCode = "#E0C68F", CreatedAt = DateTime.Now, IsActive = true },
            new EventType { EventTypeName = "Workshop", Description = "Hands-on learning workshops", IconClass = "fas fa-tools", ColorCode = "#28A745", CreatedAt = DateTime.Now, IsActive = true },
            new EventType { EventTypeName = "Seminar", Description = "Educational seminars and presentations", IconClass = "fas fa-chalkboard-teacher", ColorCode = "#17A2B8", CreatedAt = DateTime.Now, IsActive = true },
            new EventType { EventTypeName = "Networking", Description = "Professional networking events", IconClass = "fas fa-handshake", ColorCode = "#FFC107", CreatedAt = DateTime.Now, IsActive = true },
            new EventType { EventTypeName = "Social", Description = "Social gatherings and parties", IconClass = "fas fa-glass-cheers", ColorCode = "#DC3545", CreatedAt = DateTime.Now, IsActive = true },
            new EventType { EventTypeName = "Training", Description = "Professional training sessions", IconClass = "fas fa-graduation-cap", ColorCode = "#6F42C1", CreatedAt = DateTime.Now, IsActive = true }
        };
        await context.EventTypes.AddRangeAsync(eventTypes);
    }

    await context.SaveChangesAsync();
}
