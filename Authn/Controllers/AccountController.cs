using Authn.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authn.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly UserService _userService;
        public AccountController(ILogger<AccountController> logger, UserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpGet("login")]
        public IActionResult Login(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet("login/{provider}")]
        public IActionResult LoginExternal([FromRoute] string provider, [FromQuery] string returnUrl)
        {
            if (User != null && User.Identities.Any(identity => identity.IsAuthenticated))
            {
                return RedirectToAction("", "Home");
            }

            // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
            // send them to the home page instead (/).
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            var authenticationProperties = new AuthenticationProperties { RedirectUri = returnUrl };
            // authenticationProperties.SetParameter("prompt", "select_account");
            return new ChallengeResult(provider, authenticationProperties);
        }

        [HttpGet("newuser")]
        public IActionResult NewUser()
        {
            return View();
        }

        [ValidateAntiForgeryToken()]
        [Route("validate")]
        [HttpPost]
        public async Task<IActionResult> Validate(string username, string password, string returnUrl)
        {
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            ViewData["ReturnUrl"] = returnUrl;
            if (_userService.TryValidateUser(username, password, out List<Claim> claims))
            {
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var items = new Dictionary<string, string>();
                items.Add(".AuthScheme", CookieAuthenticationDefaults.AuthenticationScheme);
                var properties = new AuthenticationProperties(items);
                await HttpContext.SignInAsync(claimsPrincipal, properties);
                return Redirect(returnUrl);
            }
            else
            {
                TempData["Error"] = "Error. Username or Password is invalid";
                return View("login");
            }
        }

        [Authorize]
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            var scheme = User.Claims.FirstOrDefault(c => c.Type == ".AuthScheme").Value;
            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
            switch (scheme)
            {
                case "google":
                    await HttpContext.SignOutAsync();
                    var redirect = $"https://www.google.com/accounts/Logout?continue=https://appengine.google.com/_ah/logout?continue={domainUrl}";
                    return Redirect(redirect);
                case "facebook":
                case "Cookies":
                    await HttpContext.SignOutAsync();
                    return Redirect("/");
                case "microsoft":
                    await HttpContext.SignOutAsync();
                    return Redirect("/");
                default:
                    return new SignOutResult(new[] { CookieAuthenticationDefaults.AuthenticationScheme, scheme });
            }
        }
    }
}

