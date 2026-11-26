
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Token2Core.Data;
using WritePad.Model;

namespace WritePad.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAdminAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            await using var scope =
                serviceProvider.CreateAsyncScope();

            var context = scope.ServiceProvider
                .GetRequiredService<TokenDbContext>();

            // Check whether admin already exists
            var adminExists = await context.Users
                .AnyAsync(
                    u => u.Username == "admin",
                    cancellationToken);

            if (!adminExists)
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@writepad.com",
                    Role = "Admin"
                };

                var hasher = new PasswordHasher<User>();

                admin.PasswordHash = hasher.HashPassword(
                    admin,
                    "Admin@123");

                context.Users.Add(admin);

                await context.SaveChangesAsync(
                    cancellationToken);
            }
        }
    }
}


