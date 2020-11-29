using System;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using WebApi.Helpers;
using WebApi.Services;
using WebApi.Data;
using WebApi.Configuration;

namespace WebApi
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
            services.AddCors();
            services.AddControllers();

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("SqlConnection")));

            services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                            .AddEntityFrameworkStores<ApplicationDbContext>();

            // configure strongly typed settings objects	    
            var jwtSection = Configuration.GetSection("JwtBearerTokenSettings"); 
            services.Configure<JwtBearerTokenSettings>(jwtSection); 

            var jwtBearerTokenSettings = jwtSection.Get<JwtBearerTokenSettings>(); 
            var key = Encoding.ASCII.GetBytes(jwtBearerTokenSettings.SecretKey);

            services.AddAuthentication(options => 
            { 
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; 
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; 
            })
                .AddJwtBearer(options => 
                { 
                    options.RequireHttpsMetadata = false; 
                    options.SaveToken = true; 
                    options.TokenValidationParameters = new TokenValidationParameters() 
                    { 
                        ValidateIssuer = true, 
                        ValidIssuer = jwtBearerTokenSettings.Issuer, 
                        ValidateAudience = true, 
                        ValidAudience = jwtBearerTokenSettings.Audience, 
                        ValidateIssuerSigningKey = true, 
                        IssuerSigningKey = new SymmetricSecurityKey(key), 
                        ValidateLifetime = true, ClockSkew = TimeSpan.Zero 
                    }; 
            });

            // configure basic authentication 
            services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

            // configure DI for application services
            services.AddScoped<IUserService, UserService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting(); //make routing information available to authentication below.

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication(); //notice the before and after events.
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers()); //user is authenticated before using endpoints.
        }
    }
}
