using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;
using PassItOnAcademy.Services;      // PayFastOptions, IPayFastService, PayFastService

namespace PassItOnAcademy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Bind to your chosen URL/port
            builder.WebHost.UseUrls("http://127.0.0.1:5200");

            // --- Database ---
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // --- App services ---
            builder.Services.AddScoped<PassItOnAcademy.Services.IAuditService, PassItOnAcademy.Services.AuditService>();

            // PayFast options + services
            builder.Services.Configure<PayFastOptions>(builder.Configuration.GetSection("PayFast"));
            builder.Services.AddScoped<IPayFastService, PayFastService>();

            // ITN verifier + HttpClient (server-to-server validation)
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IPayFastItnVerifier, PayFastItnVerifier>();

            // Keep auto-cancel stale pending payments/bookings
            builder.Services.AddHostedService<PendingCleanupService>();

            // ?? Removed Email + ReminderService registrations

            // --- Identity + Roles ---
            builder.Services
                .AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // --- Pipeline ---
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
               .WithStaticAssets();

            app.MapRazorPages().WithStaticAssets();

            // ---- Seed roles & users ----
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    SeedIdentityAsync(services).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error seeding Identity data.");
                }
            }

            app.Run();
        }

        private static async Task SeedIdentityAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            const string adminRole = "Admin";
            const string coachRole = "Coach";
            const string customerRole = "Customer";

            foreach (var role in new[] { adminRole, coachRole, customerRole })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // Admin
            const string adminEmail = "zack@passitonacademy.co.za";
            const string adminPassword = "ZackAdmin!2025";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Zack Smith",
                    PhoneNumber = "0000000000"
                };
                var createResult = await userManager.CreateAsync(admin, adminPassword);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }
            foreach (var role in new[] { adminRole, coachRole })
                if (!await userManager.IsInRoleAsync(admin, role))
                    await userManager.AddToRoleAsync(admin, role);

            // Test customer
            const string custEmail = "customer@passitonacademy.co.za";
            const string custPassword = "Customer!2025";
            var cust = await userManager.FindByEmailAsync(custEmail);
            if (cust == null)
            {
                cust = new ApplicationUser
                {
                    UserName = custEmail,
                    Email = custEmail,
                    EmailConfirmed = true,
                    FullName = "Test Customer"
                };
                var createResult = await userManager.CreateAsync(cust, custPassword);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }
            if (!await userManager.IsInRoleAsync(cust, customerRole))
                await userManager.AddToRoleAsync(cust, customerRole);
        }
    }
}
