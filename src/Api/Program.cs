using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Tầng Infrastructure: DbContext + (sau này) repository, service ──
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─────────────────────────────────────────────────────────────────────
// TODO (Câu 3 — SSO): cấu hình xác thực JWT Bearer trỏ về AuthService.
//
// Sau khi cấu hình, nhớ bật middleware ở phần pipeline bên dưới:
//     app.UseAuthentication();
//     app.UseAuthorization();
//
// Cấu hình (địa chỉ AuthService, audience...) đọc từ appsettings.json,
// không hardcode trong file này.
// ─────────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
