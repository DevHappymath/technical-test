using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>
/// Nơi đăng ký các dịch vụ thuộc tầng Infrastructure vào DI container.
/// Ứng viên bổ sung thêm repository / service của mình tại đây.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Thiếu chuỗi kết nối 'DefaultConnection' trong cấu hình.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // TODO (ứng viên): đăng ký repository / service tại đây.
        // services.AddScoped<IExampleRepository, ExampleRepository>();

        return services;
    }
}
