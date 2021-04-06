using Authn.Models;
using Authn.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authn.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserService _userService;

        public HomeController(ILogger<HomeController> logger, UserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


        [HttpGet("denied")]
        public IActionResult Denied()
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Role)?.Value;
                if (role == "NewUser") // we have a new user, let's take them to a new user welcome page    
                {
                    return Redirect("/newuser");
                }
            }
            return View();
        }

        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Secured()
        {
            await Task.CompletedTask;
            //var idToken = await HttpContext.GetTokenAsync("id_token");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
