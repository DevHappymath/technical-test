# syntax=docker/dockerfile:1
#
# Build context là thư mục `src/`:
#     docker build -t booking-api -f Dockerfile .

# ── Giai đoạn build ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy riêng file project trước để tận dụng cache: chỉ khi .csproj đổi
# thì bước restore mới phải chạy lại.
COPY global.json ./
COPY TestAssessment.sln ./
COPY Api/Api.csproj Api/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
RUN dotnet restore TestAssessment.sln

COPY . .
RUN dotnet publish Api/Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ── Giai đoạn chạy ───────────────────────────────────────────────────
# Dùng image Debian (không phải alpine) vì có sẵn ICU — cần cho so sánh
# chuỗi tiếng Việt không phân biệt hoa thường ở API tìm kiếm.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Mặc định Production — Swagger và trang lỗi chi tiết chỉ bật ở Development.
# docker-compose.yml ghi đè thành Development cho môi trường làm bài.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish .

# Chạy bằng user không phải root. $APP_UID (=1654) do image .NET 8 định nghĩa sẵn.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
