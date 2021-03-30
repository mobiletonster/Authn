using Authn.Data;
using Authn.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authn
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddDbContext<AuthDbContext>(options => options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<UserService>();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/denied";
                    options.Events = new CookieAuthenticationEvents()
                    {
                        OnSigningIn = async context =>
                        {
                            var scheme = context.Properties.Items.Where(k => k.Key == ".AuthScheme").FirstOrDefault();
                            var claim = new Claim(scheme.Key, scheme.Value);
                            var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                            var userService = context.HttpContext.RequestServices.GetRequiredService(typeof(UserService)) as UserService;
                            var nameIdentifier = claimsIdentity.Claims.FirstOrDefault(m => m.Type == ClaimTypes.NameIdentifier)?.Value;
                            if (userService != null && nameIdentifier != null)
                            {
                                var appUser = userService.GetUserByExternalProvider(scheme.Value, nameIdentifier);
                                if (appUser is null)
                                {
                                    appUser = userService.AddNewUser(scheme.Value, claimsIdentity.Claims.ToList());
                                }
                                foreach (var r in appUser.RoleList)
                                {
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, r));
                                }
                            }
                            claimsIdentity.AddClaim(claim);
                            await Task.CompletedTask;
                        }
                    };
                })
                .AddOpenIdConnect("google", options =>
                {
                    options.Authority = Configuration["GoogleOpenId:Authority"];
                    options.ClientId = Configuration["GoogleOpenId:ClientId"];
                    options.ClientSecret = Configuration["GoogleOpenId:ClientSecret"];
                    options.CallbackPath = Configuration["GoogleOpenId:CallbackPath"];
                    options.SignedOutCallbackPath = Configuration["GoogleOpenId:SignedOutCallbackPath"];
                    options.SaveTokens = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // map name claim to ClaimTypes.Name
                        NameClaimType = "name",
                    };
                }).AddOpenIdConnect("okta", options =>
                {
                    options.Authority = Configuration["OktaOpenId:Authority"];
                    options.ClientId = Configuration["OktaOpenId:ClientId"];
                    options.ClientSecret = Configuration["OktaOpenId:ClientSecret"];
                    options.CallbackPath = Configuration["OktaOpenId:CallbackPath"];
                    options.SignedOutCallbackPath = Configuration["OktaOpenId:SignedOutCallbackPath"];
                    options.ResponseType = "code";
                    options.SaveTokens = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                })
                .AddFacebook("facebook", options =>
                {
                    options.AppId = Configuration["FBOauth:AppId"];
                    options.AppSecret = Configuration["FBOauth:ClientSecret"];
                    options.ClientSecret = Configuration["FBOauth:ClientSecret"];
                    options.CallbackPath = Configuration["FBOauth:CallbackPath"];
                });



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => 
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            
        }
    }
}
