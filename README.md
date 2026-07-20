# Bài Test Kỹ Thuật Backend — GDTD

Chào bạn, cảm ơn bạn đã dành thời gian cho bài test này.

## Bắt đầu từ đâu

| Bước | Việc cần làm |
|---|---|
| 1 | Đọc **[docs/01-yeu-cau.md](docs/01-yeu-cau.md)** — toàn bộ đề bài (3 câu) |
| 2 | Dựng môi trường theo hướng dẫn bên dưới, chạy thử `/api/health` để chắc chắn mọi thứ hoạt động |
| 3 | Bắt đầu làm bài trong thư mục `src/` |
| 4 | Viết `SOLUTION.md` — **bắt buộc**, xem yêu cầu nội dung ở cuối file đề bài |

## Yêu cầu môi trường

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (để chạy PostgreSQL)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Dựng môi trường

```bash
cd src

# 1. Khởi động PostgreSQL (cổng 5433 để không đụng DB có sẵn trên máy bạn)
docker compose up -d

# 2. Chạy API
dotnet run --project Api
```

Mở trình duyệt: **http://localhost:5080/swagger**

### Kiểm tra môi trường đã sẵn sàng

```bash
curl http://localhost:5080/api/health
# → {"status":"healthy","timestamp":"..."}

curl http://localhost:5080/api/health/database
# → {"status":"healthy","database":"connected"}
```

Nếu cả hai endpoint đều trả `healthy`, môi trường của bạn đã sẵn sàng.

### Tạo và chạy migration

```bash
cd src

dotnet ef migrations add TenMigration \
    --project Infrastructure \
    --startup-project Api \
    --output-dir Persistence/Migrations

dotnet ef database update \
    --project Infrastructure \
    --startup-project Api
```

### Kết nối trực tiếp vào database (khi cần kiểm tra dữ liệu)

```bash
docker exec -it booking-test-db psql -U postgres -d booking_test
```

| Thông số | Giá trị |
|---|---|
| Host | `localhost` |
| Port | `5433` |
| Database | `booking_test` |
| Username / Password | `postgres` / `postgres` |

## Khung sườn có sẵn

```
src/
├── TestAssessment.sln
├── docker-compose.yml          PostgreSQL 16
├── global.json                 ghim .NET SDK 8
├── Api/                        Web API — Program.cs, HealthController
├── Application/                (rỗng)
├── Domain/                     (rỗng)
└── Infrastructure/
    ├── DependencyInjection.cs  đăng ký DbContext
    └── Persistence/
        └── AppDbContext.cs     (rỗng — bạn tự khai báo DbSet)
```

Bốn project đã tham chiếu lẫn nhau theo hướng: `Api → Application/Infrastructure → Domain`.

> [!IMPORTANT]
> **Các thư mục để trống là có chủ đích.** Chúng tôi không áp đặt bạn phải tổ chức code theo cấu trúc nào — cách chia thư mục, chia tầng, chọn thư viện (AutoMapper, FluentValidation, MediatR, Dapper...) hoàn toàn do bạn quyết định.
>
> Bạn cũng có thể thay đổi khung sườn này nếu thấy cần. Chỉ cần giải thích lý do trong `SOLUTION.md`.

## Một vài lưu ý

- **Làm không hết vẫn nộp.** Chất lượng phần làm được quan trọng hơn số lượng câu hoàn thành.
- **Ghi lại phần chưa xong** trong `SOLUTION.md` — trung thực là một tiêu chí được đánh giá, không phải điểm trừ.
- **Được dùng AI** (ChatGPT, Claude, Copilot...). Nhưng buổi phỏng vấn sẽ hỏi trực tiếp về code bạn nộp, nên hãy chắc chắn bạn hiểu những gì mình viết.
- **Có gì chưa rõ trong đề, cứ hỏi.** Biết đặt câu hỏi làm rõ yêu cầu là điểm cộng.

Chúc bạn làm bài tốt!
