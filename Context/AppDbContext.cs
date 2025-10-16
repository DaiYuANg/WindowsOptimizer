using Microsoft.EntityFrameworkCore;

namespace WindowsOptimizer.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<SystemLog> SystemLogs { get; set; }
    public DbSet<UserSetting> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // 可以在这里配置表名、字段属性等
    }
}

public class SystemLog
{
    public int Id { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}

public class UserSetting
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
}