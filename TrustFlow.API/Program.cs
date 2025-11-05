
using Microsoft.Extensions.Options;
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

            builder.Services.AddControllers();
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

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
