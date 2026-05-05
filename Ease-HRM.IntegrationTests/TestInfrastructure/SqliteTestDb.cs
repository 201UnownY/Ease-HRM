using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.TestInfrastructure;

internal static class SqliteTestDb
{
    public static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
        connection.Open();
        return connection;
    }

    public static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SqliteTestAppDbContext(options);
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw("CREATE TRIGGER IF NOT EXISTS TRG_LeaveBalances_RowVersion_Update AFTER UPDATE ON LeaveBalances FOR EACH ROW BEGIN UPDATE LeaveBalances SET RowVersion = randomblob(8) WHERE Id = NEW.Id; END;");
        return context;
    }

    private sealed class SqliteTestAppDbContext : AppDbContext
    {
        public SqliteTestAppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Set default RowVersion for all concurrency-tracked entities in SQLite tests
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
}
