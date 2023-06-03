using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ComeAndTicketBlazor.Data;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace ComeAndTicketBlazor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            IConfigurationSection dbAuthSection = Configuration.GetSection("Authentication:Database");
            string userName = dbAuthSection["UserName"];
            string password = dbAuthSection["Password"];
            var context = new ComeAndTicketContext(userName, password);
            services.AddSingleton<ComeAndTicketContext>(context);

            services.AddSingleton<IComeAndTicketDataService, ComeAndTicketDataService>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
            services.AddAuthentication().AddGoogle(options =>
            {
                IConfigurationSection googleAuthSection = Configuration.GetSection("Authentication:Google");

                options.ClientId = googleAuthSection["ClientId"];
                options.ClientSecret = googleAuthSection["ClientSecret"];
                options.ClaimActions.MapJsonKey("urn:google:profile", "link");
                options.ClaimActions.MapJsonKey("urn:google:image", "picture");
            });

            // From: https://github.com/aspnet/Blazor/issues/1554
            // Adds HttpContextAccessor used to determine if a user is logged in and what their username is
            services.AddHttpContextAccessor();
            services.AddScoped<HttpContextAccessor>();

            // Required for HttpClient support in the Blazor Client project
            services.AddHttpClient();
            services.AddScoped<HttpClient>();

            // Pass settings to other components
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddResponseCompression();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
