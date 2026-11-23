
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Token2Core.Data;

namespace WritePad.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "WritePad_Test_Secret_Key_12345678901234567890",
                ["Jwt:Issuer"] = "WritePadTest",
                ["Jwt:Audience"] = "WritePadTestUsers"
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TokenDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<TokenDbContext>>();
            services.RemoveAll<TokenDbContext>();

            services.AddDbContext<TokenDbContext>(options =>
            {
                options.UseInMemoryDatabase($"WritePadTestDb_{Guid.NewGuid()}");
            });
        });
    }
}
