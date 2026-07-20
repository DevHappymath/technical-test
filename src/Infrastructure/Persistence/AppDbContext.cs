using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// DbContext của ứng dụng.
///
/// Hiện tại đang rỗng — ứng viên tự khai báo DbSet và cấu hình mapping
/// cho các entity của mình.
///
/// Gợi ý: <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/> đã được gọi sẵn
/// ở dưới, nên mọi lớp cấu hình implement IEntityTypeConfiguration&lt;T&gt; trong
/// project này sẽ được nạp tự động — không cần đăng ký thủ công từng lớp.
/// Nếu muốn cấu hình theo cách khác (Data Annotations, Fluent API trực tiếp
/// trong OnModelCreating...) thì cứ tự nhiên, không bắt buộc theo gợi ý này.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
