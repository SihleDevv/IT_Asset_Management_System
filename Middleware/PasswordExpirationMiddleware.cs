using Microsoft.AspNetCore.Identity;
using IT_Asset_Management_System.Models;

namespace IT_Asset_Management_System.Middleware
{
    public class PasswordExpirationMiddleware
    {
        private readonly RequestDelegate _next;

        public PasswordExpirationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            // Skip check for anonymous users or specific paths
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    // Check if password has expired (30 days)
                    bool passwordExpired = false;
                    if (user.PasswordChangedDate.HasValue)
                    {
                        var daysSinceChange = (DateTime.Now - user.PasswordChangedDate.Value).TotalDays;
                        if (daysSinceChange >= 30)
                        {
                            passwordExpired = true;
                            user.MustChangePassword = true;
                            await userManager.UpdateAsync(user);
                        }
                    }
                    else
                    {
                        // If PasswordChangedDate is null, mark as expired
                        passwordExpired = true;
                        user.MustChangePassword = true;
                        await userManager.UpdateAsync(user);
                    }

                    // Redirect to change password page if expired (except for the change password page itself and logout)
                    var path = context.Request.Path.Value?.ToLower() ?? "";
                    if ((passwordExpired || user.MustChangePassword) && 
                        !path.Contains("/account/changeexpiredpassword") && 
                        !path.Contains("/account/logout") &&
                        !path.Contains("/account/login"))
                    {
                        context.Response.Redirect("/Account/ChangeExpiredPassword");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
