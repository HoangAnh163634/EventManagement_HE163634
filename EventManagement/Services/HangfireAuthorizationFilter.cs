using Hangfire.Dashboard;

namespace EventManagement.Services;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var userRole = httpContext.Session.GetString("UserRole");
        return userRole == "Admin";
    }
} 