using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ease_HRM.IntegrationTests.TestInfrastructure;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly TimeSpan? _jwtTokenLifetime;
    private SqliteConnection? _connection;

    public ApiWebApplicationFactory(TimeSpan? jwtTokenLifetime = null)
    {
        _jwtTokenLifetime = jwtTokenLifetime;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor is not null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            _connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            _connection.Open();

            services.AddSingleton<DbConnection>(_connection);
            services.AddScoped<AppDbContext>(sp =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(sp.GetRequiredService<DbConnection>())
                    .Options;

                return new SqliteWebApplicationDbContext(options);
            });

            if (_jwtTokenLifetime.HasValue)
            {
                var tokenServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITokenService));
                if (tokenServiceDescriptor is not null)
                {
                    services.Remove(tokenServiceDescriptor);
                }

                services.AddScoped<ITokenService>(sp =>
                    new TestTokenService(sp.GetRequiredService<IConfiguration>(), _jwtTokenLifetime.Value));
            }

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            ConfigureSqliteRowVersionSupport(db);
        });
    }

    public async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private static void ConfigureSqliteRowVersionSupport(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("CREATE TRIGGER IF NOT EXISTS TRG_Employees_RowVersion_Update AFTER UPDATE ON Employees FOR EACH ROW BEGIN UPDATE Employees SET RowVersion = randomblob(8) WHERE Id = NEW.Id; END;");
        db.Database.ExecuteSqlRaw("CREATE TRIGGER IF NOT EXISTS TRG_LeaveBalances_RowVersion_Update AFTER UPDATE ON LeaveBalances FOR EACH ROW BEGIN UPDATE LeaveBalances SET RowVersion = randomblob(8) WHERE Id = NEW.Id; END;");
        db.Database.ExecuteSqlRaw("CREATE TRIGGER IF NOT EXISTS TRG_LeaveRequests_RowVersion_Update AFTER UPDATE ON LeaveRequests FOR EACH ROW BEGIN UPDATE LeaveRequests SET RowVersion = randomblob(8) WHERE Id = NEW.Id; END;");
        db.Database.ExecuteSqlRaw("CREATE TRIGGER IF NOT EXISTS TRG_Payrolls_RowVersion_Update AFTER UPDATE ON Payrolls FOR EACH ROW BEGIN UPDATE Payrolls SET RowVersion = randomblob(8) WHERE Id = NEW.Id; END;");
    }

    private sealed class SqliteWebApplicationDbContext : AppDbContext
    {
        public SqliteWebApplicationDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Ease_HRM.Domain.Entities.Employee>()
                .Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("randomblob(8)");

            builder.Entity<Ease_HRM.Domain.Entities.LeaveBalance>()
                .Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("randomblob(8)");

            builder.Entity<Ease_HRM.Domain.Entities.LeaveRequest>()
                .Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("randomblob(8)");

            builder.Entity<Ease_HRM.Domain.Entities.Payroll>()
                .Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("randomblob(8)");
        }
    }

    private sealed class TestTokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _tokenLifetime;

        public TestTokenService(IConfiguration configuration, TimeSpan tokenLifetime)
        {
            _configuration = configuration;
            _tokenLifetime = tokenLifetime;
        }

        public string GenerateToken(User user, IEnumerable<string> permissions)
        {
            var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email)
            };

            claims.AddRange(permissions
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(permission => new Claim("permission", permission)));

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var expiresAt = GetExpirationUtc();
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetExpirationUtc()
        {
            return DateTime.UtcNow.Add(_tokenLifetime);
        }
    }
}
