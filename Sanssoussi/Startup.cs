using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Google;

namespace Sanssoussi
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: "Corsingtime",
                                  builder =>
                                  {
                                      builder.WithOrigins("https://localhost:5001")
                                             .AllowAnyHeader()
                                             .AllowAnyMethod();
                                  });
            });
            services.AddRazorPages();
            services.AddControllersWithViews();

            services.AddAuthentication()
            .AddCookie()
            .AddGoogle(options =>
            {
                IConfigurationSection googleAuth = Configuration.GetSection("Authentication:Google");
                options.ClientId = googleAuth["ClientId"];
                options.ClientSecret = googleAuth["ClientSecret"];
            });

            services.AddLogging();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();  // This middleware adds the Strict-Transport-Security header to HTTP responses.
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("Corsingtime"); // Apply the CORS policy

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCookiePolicy();


            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                    endpoints.MapRazorPages();
                });
        }
    }
}