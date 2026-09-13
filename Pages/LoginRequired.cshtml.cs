using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _10x_cards.Pages;

[AllowAnonymous]
public class LoginRequiredModel : PageModel
{
    public bool IsDevelopment { get; private set; }

    public void OnGet()
    {
        IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}
