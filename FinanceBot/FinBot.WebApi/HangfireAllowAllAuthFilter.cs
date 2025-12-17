using Hangfire.Dashboard;

namespace FinBot.WebApi;

public class HangfireAllowAllAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}