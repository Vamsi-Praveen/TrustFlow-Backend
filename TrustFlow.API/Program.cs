
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TrustFlow.Core.Data;
using TrustFlow.Core.Helpers;
using TrustFlow.Core.Models;
using TrustFlow.Core.Seed;
using TrustFlow.Core.Services;

namespace TrustFlow.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

            builder.Services.AddSingleton(sp =>
            {
                var mongoSettings = builder.Configuration
                    .GetSection("MongoDbSettings")
                    .Get<MongoDbSettings>();
                return new ApplicationContext(mongoSettings);
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/api/users/authenticate";
                    options.AccessDeniedPath = "/api/users/access-denied";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = ctx =>
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = ctx =>
                        {
                            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddTransient<DbSeeder>();
            builder.Services.AddSingleton<PasswordHelper>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ProjectService>();
            builder.Services.AddScoped<RolePermissionService>();
            builder.Services.AddScoped<SlackService>();
            builder.Services.AddScoped<TeamsService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<SystemSettingService>();
            builder.Services.AddScoped<IssueService>();
            builder.Services.AddScoped<ActivityService>();
            builder.Services.AddScoped<LogService>();
            builder.Services.AddScoped<ProjectDetailsService>();

            // Register the IConnectionMultiplexer
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = builder.Configuration.GetSection("Redis")["ConnectionString"];
                return ConnectionMultiplexer.Connect(configuration);
            });

            // Register your RedisCacheService
            builder.Services.AddSingleton<RedisCacheService>();
            builder.Services.AddScoped<AzureBlobService>();

            var app = builder.Build();

            //Seed data
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var mongoDbSettings = services.GetRequiredService<IOptions<MongoDbSettings>>().Value;

                if (mongoDbSettings.SeedData)
                {
                    var seeder = services.GetRequiredService<DbSeeder>();
                    await seeder.SeedAsync();
                }
            }


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
