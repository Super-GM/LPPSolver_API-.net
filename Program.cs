using Lpp_Solver.services;
using Microsoft.Extensions.Options;

namespace Lpp_Solver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. SERVICES CONFIGURATION
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Dependency Injection Setup
            builder.Services.AddScoped<ILPSolverservice, LpSolverService>();
            builder.Services.AddCors(Options => {
                Options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin()
                                                                                                .AllowAnyMethod()
                                                                                                .AllowAnyHeader());
            });

            var app = builder.Build();
            app.UseCors("AllowAll");

            // 2. MIDDLEWARE PIPELINE (Order is CRITICAL)

            // Must come first for production apps (before UseRouting)
            app.UseHttpsRedirection();

            // Enables serving files from wwwroot (CSS, JS, Images)
            app.UseStaticFiles();

            // ⚠️ Step 1: Defines where endpoints are found (must be before UseRouting)
            app.UseRouting();

            // (Security middleware goes here)
            app.UseAuthorization();

            // ⚠️ Step 2: Executes the endpoints (Mapping)
            // The order of mapping matters: Razor Pages is usually last to catch any non-API routes.

            // Maps API Controllers (e.g., api/LPPsolver/...)
            app.MapControllers();

            // Maps Razor Pages (e.g., / or /Index)
            app.MapRazorPages();

            // 💡 Optional: Uncomment if you want to use Swagger for testing the API
            // if (app.Environment.IsDevelopment())
            // {
            //     app.UseSwagger();
            //     app.UseSwaggerUI();
            // }

            // app.UseWelcomePage(); // No longer needed as MapRazorPages should serve Index.cshtml

            app.Run();
        }
    }
}