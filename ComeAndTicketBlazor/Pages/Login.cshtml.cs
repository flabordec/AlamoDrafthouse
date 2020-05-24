using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ComeAndTicketBlazor.Pages
{
    public class LoginModel : PageModel
    {
        public IActionResult OnGetAsync(string returnUrl = null)
        {
            string provider = "Google";
            // Request a redirect to the external login provider.
            var redirectUri = Url.Page("./Login", pageHandler: "Callback", values: new { returnUrl });
            var authenticationProperties = new AuthenticationProperties
            {
                RedirectUri = redirectUri,
            };

            return new ChallengeResult(provider, authenticationProperties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {

            // Get the information about the user from the external login provider

            var GoogleUser = this.User.Identities.FirstOrDefault();

            if (GoogleUser.IsAuthenticated)
            {
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    RedirectUri = Request.Host.Value
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(GoogleUser), 
                    authProperties);

            }

            return LocalRedirect("/");

        }

    }
}
