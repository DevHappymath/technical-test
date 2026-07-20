using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Endpoint kiểm tra hệ thống chạy được và kết nối được database.
/// Dùng để xác nhận môi trường đã sẵn sàng trước khi bắt đầu làm bài.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>GET /api/health — kiểm tra API có sống không.</summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>GET /api/health/database — kiểm tra kết nối tới PostgreSQL.</summary>
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseAsync(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                message = "Không kết nối được tới database. Kiểm tra lại `docker compose up -d` và chuỗi kết nối trong appsettings.json."
            });
        }

        return Ok(new
        {
            status = "healthy",
            database = "connected"
        });
    }
}
