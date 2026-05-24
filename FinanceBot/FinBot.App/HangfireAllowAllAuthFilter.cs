using Hangfire.Dashboard;

namespace FinBot.App;

public class HangfireAllowAllAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}