# Bài Test Kỹ Thuật Backend — Hệ sinh thái GDTD

> **Vị trí:** Backend Developer (Junior — 0-2 năm kinh nghiệm)
> **Công nghệ:** .NET 8 · Entity Framework Core · PostgreSQL · Docker
> **Thời gian:** khoảng 2 ngày làm việc (take-home — bạn tự sắp xếp, không bấm giờ)
> **Nộp bài:** Xem mục [Hướng dẫn nộp bài](#hướng-dẫn-nộp-bài) ở cuối tài liệu

---

## 0. Giới thiệu

Hệ sinh thái GDTD gồm nhiều service độc lập (CRM, HRM, LMS, Hub, Student, Teacher...), tất cả đăng nhập tập trung qua một **AuthService** dùng chuẩn OpenID Connect.

Bài test này mô phỏng một task thực tế bạn sẽ nhận trong tuần đầu đi làm: xây dựng tính năng **Quản lý đặt phòng học** cho một service mới, từ thiết kế cơ sở dữ liệu → CRUD → xử lý nghiệp vụ → tích hợp đăng nhập SSO.

### Bối cảnh nghiệp vụ

Trung tâm có nhiều phòng học. Giáo viên và nhân viên cần **đặt phòng trước** để dạy bù, họp chuyên môn, tổ chức sự kiện. Quản lý cơ sở vật chất sẽ **duyệt hoặc từ chối** các yêu cầu này. Hiện tại mọi thứ đang làm thủ công trên Excel, dẫn tới trùng lịch và tranh chấp phòng.

Nhiệm vụ của bạn: xây dựng API backend cho tính năng này.

### Những gì đã được chuẩn bị sẵn

Thư mục `src/` chứa một khung sườn tối giản đã chạy được:

- Solution `TestAssessment.sln` với 4 project: `Api`, `Application`, `Domain`, `Infrastructure` (đã tham chiếu lẫn nhau)
- `docker-compose.yml` dựng sẵn PostgreSQL
- `Dockerfile` + `.dockerignore` để đóng gói API (không bắt buộc dùng — xem bên dưới)
- `AppDbContext` rỗng, đã đăng ký vào DI
- `HealthController` để kiểm tra hệ thống chạy được

Hai cách chạy, chọn cách nào cũng được:

```bash
# Cách 1 — database trong Docker, API chạy ngoài (dễ debug hơn)
docker compose up -d
dotnet run --project Api

# Cách 2 — chạy cả hai trong Docker
docker compose --profile app up -d --build   # API tại http://localhost:5080
```

> [!IMPORTANT]
> **Khung sườn cố tình để trống.** Chúng tôi **không** áp đặt bạn phải tổ chức code theo cách nào — đặt thư mục, chia tầng, tự chọn thư viện (AutoMapper, FluentValidation, MediatR, Dapper...) đều do bạn quyết định. Chúng tôi quan tâm **lý do** bạn chọn cách đó, và mong bạn giải thích ngắn gọn trong file `SOLUTION.md`.

### Yêu cầu chung (áp dụng cho cả 3 câu)

| # | Yêu cầu |
|---|---|
| 1 | Code chạy được: `docker compose up -d` → `dotnet run` → gọi API thành công |
| 2 | Dùng **EF Core Migrations** để tạo bảng (không viết SQL tay, không dùng `EnsureCreated`) |
| 3 | API trả JSON với format thống nhất giữa các endpoint (kể cả khi lỗi) |
| 4 | HTTP status code đúng ngữ nghĩa (200/201/400/401/403/404/409...) |
| 5 | Thông báo lỗi bằng **tiếng Việt**, rõ ràng cho người dùng cuối |
| 6 | Không hardcode connection string / secret trong code — dùng `appsettings.json` hoặc biến môi trường |
| 7 | Ghi lại quyết định thiết kế và phần chưa làm xong vào `SOLUTION.md` |

> [!TIP]
> **Làm không hết vẫn nộp.** Chúng tôi đánh giá chất lượng phần bạn làm được cao hơn số lượng. Một Câu 1 làm sạch sẽ, đúng chuẩn tốt hơn cả 3 câu làm dở dang. Hãy ghi rõ phần chưa xong trong `SOLUTION.md`.

---

## Câu 1 — Thiết kế CSDL & CRUD Phòng học

**Trọng số: 40 điểm**

Xây dựng chức năng quản lý danh mục phòng học.

### 1.1. Thiết kế bảng

Thiết kế bảng lưu thông tin phòng học với các thông tin nghiệp vụ sau:

| Thông tin | Mô tả | Ràng buộc nghiệp vụ |
|---|---|---|
| Mã phòng | Định danh phòng do trung tâm đặt | Bắt buộc, **không trùng nhau**, tối đa 20 ký tự. VD: `A101`, `B205` |
| Tên phòng | Tên hiển thị | Bắt buộc, tối đa 200 ký tự. VD: `Phòng Toán 1` |
| Sức chứa | Số người tối đa | Bắt buộc, số nguyên dương, từ 1 đến 500 |
| Cơ sở | Địa điểm vật lý | Bắt buộc, tối đa 200 ký tự. VD: `Cơ sở Cầu Giấy` |
| Mô tả | Ghi chú thêm (thiết bị sẵn có...) | Không bắt buộc, tối đa 1000 ký tự |
| Trạng thái | Tình trạng khai thác | Bắt buộc, chỉ nhận 1 trong 3 giá trị: `ACTIVE` (đang dùng), `INACTIVE` (ngưng dùng), `MAINTENANCE` (đang bảo trì) |

Ngoài ra, mỗi bản ghi cần có **khóa chính** và các trường **audit** (thời điểm tạo, thời điểm cập nhật gần nhất).

**Yêu cầu:**

- Định nghĩa entity trong tầng Domain
- Cấu hình mapping sang database: tên bảng, tên cột, kiểu dữ liệu, độ dài, `NOT NULL`, index
- Tạo migration và chạy được lên PostgreSQL

**Câu hỏi thiết kế** — trả lời ngắn gọn trong `SOLUTION.md`, mỗi câu 2-4 dòng:

1. Bạn chọn kiểu dữ liệu gì cho **khóa chính** (`int` tự tăng / `Guid`)? Nêu ưu nhược điểm của lựa chọn đó.
2. Bạn lưu **trạng thái** xuống database dưới dạng gì (`int` / `string`)? Nêu ưu nhược điểm.
3. Bạn tạo **index** trên những cột nào? Vì sao những cột đó mà không phải cột khác?
4. Trường thời gian nên lưu theo **UTC hay giờ Việt Nam**? Giải thích lựa chọn của bạn.

### 1.2. API CRUD

Xây dựng đầy đủ các API sau:

| Chức năng | Mô tả & yêu cầu |
|---|---|
| **Tạo phòng** | Validate đầy đủ theo bảng ràng buộc ở 1.1. Mã phòng trùng → báo lỗi rõ ràng, **không** để lỗi từ database văng thẳng ra ngoài |
| **Cập nhật phòng** | Không tìm thấy → 404. Đổi mã phòng thành mã đã tồn tại ở phòng khác → báo lỗi |
| **Xem chi tiết** | Trả về đầy đủ thông tin 1 phòng theo định danh |
| **Danh sách (phân trang)** | Xem chi tiết yêu cầu bên dưới |
| **Xóa phòng** | Xem chi tiết yêu cầu bên dưới |

**Chi tiết API danh sách:**

- Phân trang: nhận `pageNumber` (mặc định 1) và `pageSize` (mặc định 20, **tối đa 100**)
- Tìm kiếm theo từ khóa: khớp trên **mã phòng hoặc tên phòng**, **không phân biệt hoa thường**
- Lọc theo trạng thái
- Lọc theo cơ sở
- Kết quả trả về phải kèm **tổng số bản ghi** để frontend dựng được thanh phân trang
- Có thứ tự sắp xếp ổn định (kết quả trang 2 không được lặp lại bản ghi ở trang 1)

**Chi tiết API xóa:**

Phòng học có thể đã được đặt lịch (Câu 2). Bạn hãy tự quyết định và **giải thích trong `SOLUTION.md`**:

- Xóa cứng (xóa hẳn khỏi database) hay xóa mềm (đánh dấu đã xóa)?
- Nếu phòng đang có lịch đặt trong tương lai thì xử lý thế nào — chặn không cho xóa, hay vẫn cho xóa?
- Lựa chọn của bạn ảnh hưởng gì tới dữ liệu lịch sử đã đặt phòng?

> [!NOTE]
> Không có đáp án duy nhất đúng ở phần này. Chúng tôi chấm **lập luận** của bạn, không chấm việc bạn chọn phương án nào.

---

## Câu 2 — Nghiệp vụ Đặt phòng

**Trọng số: 40 điểm**

Xây dựng chức năng đặt phòng và luồng duyệt yêu cầu.

### 2.1. Thiết kế bảng đặt phòng

Thiết kế bảng lưu yêu cầu đặt phòng, cần thể hiện được:

| Nhóm thông tin | Chi tiết |
|---|---|
| Phòng được đặt | Liên kết tới bảng phòng học ở Câu 1 |
| Người đặt | Định danh người dùng (lấy từ token đăng nhập — xem Câu 3) |
| Thời gian | Thời điểm bắt đầu và thời điểm kết thúc |
| Mục đích sử dụng | Bắt buộc, tối đa 500 ký tự. VD: `Dạy bù lớp Toán 6A` |
| Số người tham gia | Bắt buộc, số nguyên dương |
| Trạng thái | `PENDING` (chờ duyệt) · `APPROVED` (đã duyệt) · `REJECTED` (bị từ chối) · `CANCELLED` (đã hủy) |
| Thông tin duyệt | Ai duyệt, duyệt lúc nào, lý do từ chối (nếu từ chối) |
| Thông tin hủy | Hủy lúc nào |

Kèm khóa chính và các trường audit như Câu 1.

**Yêu cầu:** Thiết lập đúng **quan hệ khóa ngoại** với bảng phòng học, và cấu hình **hành vi khi xóa** (`DeleteBehavior`) phù hợp với quyết định bạn đưa ra ở phần API xóa của Câu 1.

### 2.2. Quy tắc nghiệp vụ

Hệ thống cần thực thi các quy tắc sau. Làm được tới đâu ghi lại tới đó trong `SOLUTION.md`:

#### Khi tạo yêu cầu đặt phòng

| Mã | Quy tắc |
|---|---|
| **R1** | Thời điểm kết thúc phải **sau** thời điểm bắt đầu |
| **R2** | Thời điểm bắt đầu phải ở **tương lai** — không cho đặt phòng trong quá khứ |
| **R3** | Thời lượng đặt tối thiểu **30 phút**, tối đa **4 tiếng** |
| **R4** | Chỉ được đặt phòng đang ở trạng thái `ACTIVE`. Phòng `INACTIVE` hoặc `MAINTENANCE` → từ chối |
| **R5** | Số người tham gia **không được vượt quá sức chứa** của phòng |
| **R6** | **Không được trùng lịch**: cùng một phòng, khoảng thời gian mới không được giao với bất kỳ yêu cầu nào đang ở trạng thái `PENDING` hoặc `APPROVED`. Yêu cầu đã `REJECTED` hoặc `CANCELLED` thì **không tính** |
| **R7** | Mỗi người dùng chỉ được có tối đa **3 yêu cầu đang chờ duyệt** cùng lúc. Vượt quá → từ chối |

> [!IMPORTANT]
> **Về R6 — định nghĩa "trùng lịch":**
> Hai khoảng thời gian `[A_start, A_end)` và `[B_start, B_end)` được coi là trùng nhau khi `A_start < B_end` **và** `B_start < A_end`.
>
> Lưu ý điểm cuối là **mở**: một yêu cầu kết thúc lúc 10:00 và một yêu cầu bắt đầu lúc 10:00 **không** bị coi là trùng — hai lịch này hợp lệ, đặt sát nhau.

#### Luồng chuyển trạng thái

```
                  ┌──────────────┐
                  │   PENDING    │  ← trạng thái khi vừa tạo
                  └──────┬───────┘
          ┌──────────────┼──────────────┐
       duyệt          từ chối          hủy
          │              │              │
          ▼              ▼              ▼
   ┌────────────┐  ┌───────────┐  ┌─────────────┐
   │  APPROVED  │  │ REJECTED  │  │  CANCELLED  │
   └──────┬─────┘  └───────────┘  └─────────────┘
          │           (kết thúc)      (kết thúc)
         hủy
          │
          ▼
   ┌─────────────┐
   │  CANCELLED  │
   └─────────────┘
```

| Mã | Quy tắc |
|---|---|
| **R8** | Chỉ yêu cầu đang `PENDING` mới được duyệt hoặc từ chối |
| **R9** | Được hủy yêu cầu đang ở trạng thái `PENDING` **hoặc** `APPROVED` |
| **R10** | `REJECTED` và `CANCELLED` là trạng thái **kết thúc** — không chuyển đi đâu được nữa. `APPROVED` không được quay ngược về `PENDING` |
| **R11** | Chỉ được hủy khi còn **ít nhất 2 tiếng** trước giờ bắt đầu. Sát giờ hơn → từ chối hủy |
| **R12** | Khi từ chối, **bắt buộc** phải nhập lý do (tối đa 500 ký tự) |
| **R13** | Khi duyệt hoặc từ chối, phải lưu lại **ai** thực hiện và **thời điểm** thực hiện |

#### Phân quyền thao tác

| Mã | Quy tắc |
|---|---|
| **R14** | Chỉ **người tạo** yêu cầu mới được hủy yêu cầu đó. Người khác hủy → lỗi 403 |
| **R15** | Chỉ người có vai trò quản lý (`Manager` hoặc `Administrator`) mới được duyệt / từ chối |
| **R16** | Người dùng thường chỉ xem được yêu cầu **của chính mình**. Quản lý xem được **tất cả** |

> [!NOTE]
> R14-R16 cần thông tin người dùng lấy từ token đăng nhập ở **Câu 3**. Nếu bạn chưa làm tới Câu 3, cứ tạm lấy `userId` và `role` từ một service giả lập (hardcode) để vẫn thể hiện được logic phân quyền — cách này hoàn toàn được chấp nhận, chỉ cần ghi lại trong `SOLUTION.md`.

### 2.3. API cần xây dựng

| Chức năng | Yêu cầu |
|---|---|
| **Tạo yêu cầu đặt phòng** | Áp dụng R1-R7 |
| **Xem chi tiết một yêu cầu** | Áp dụng R16. Trả kèm thông tin phòng (mã, tên) để frontend không phải gọi thêm API |
| **Danh sách yêu cầu của tôi** | Phân trang, lọc theo trạng thái và theo khoảng thời gian |
| **Danh sách tất cả yêu cầu** | Dành cho quản lý (R15, R16). Phân trang, lọc theo phòng / trạng thái / khoảng thời gian |
| **Duyệt yêu cầu** | Áp dụng R8, R10, R13, R15 |
| **Từ chối yêu cầu** | Áp dụng R8, R10, R12, R13, R15 |
| **Hủy yêu cầu** | Áp dụng R9, R10, R11, R14 |

**Về thông báo lỗi:** khi một quy tắc bị vi phạm, hãy trả về thông báo riêng cho quy tắc đó bằng tiếng Việt, đủ để người dùng cuối hiểu chuyện gì đang xảy ra:

- Chưa rõ nghĩa: `"Yêu cầu không hợp lệ"`, `"Bad Request"`
- Rõ nghĩa: `"Phòng A101 đã có lịch đặt từ 09:00 đến 11:00 ngày 20/07/2026"`
- Rõ nghĩa: `"Số người tham gia (45) vượt quá sức chứa của phòng (30)"`
- Rõ nghĩa: `"Chỉ được hủy trước giờ bắt đầu ít nhất 2 tiếng"`

### 2.4. Câu hỏi phân tích

Trả lời trong `SOLUTION.md`. **Chỉ cần trả lời bằng lời, không cần code.**

**a) Tình huống tranh chấp**

Hai giáo viên cùng bấm nút đặt **phòng A101, 09:00-11:00 ngày mai** vào đúng **cùng một thời điểm**. Hai request chạy song song trên server.

Giả sử code kiểm tra trùng lịch (R6) của bạn viết theo trình tự tự nhiên nhất:

```
1. Query database: khoảng thời gian này đã có ai đặt chưa?
2. Không có ai → tạo bản ghi mới
3. Lưu xuống database
```

1. Điều gì xảy ra nếu **cả hai request cùng chạy tới bước 1 trước khi bất kỳ request nào chạy tới bước 3**? Mô tả kết quả cuối cùng trong database.
2. Vì sao lỗi này **rất khó phát hiện** khi bạn test thủ công một mình?
3. Nêu **ít nhất một cách** để ngăn chặn, và giải thích cách đó hoạt động ra sao.
   *Gợi ý hướng tìm hiểu: ràng buộc unique ở tầng database, transaction, khóa bi quan / khóa lạc quan.*

> [!NOTE]
> **Không cần triển khai** cách bạn nêu ở ý 3. Chúng tôi chấm khả năng **nhận ra** vấn đề — đây là kỹ năng quan trọng hơn nhiều so với việc biết sẵn cách sửa.

**b) Vấn đề truy vấn**

API "Danh sách tất cả yêu cầu" hiển thị kèm **mã phòng và tên phòng** của mỗi yêu cầu. Một trang có 20 yêu cầu.

1. Code của bạn thực tế bắn xuống database **bao nhiêu câu query** để lấy đủ dữ liệu cho trang đó?
2. Nếu con số đó là 21 (1 query lấy danh sách + 20 query lấy tên phòng) thì vấn đề này tên là gì, và khắc phục thế nào?

---

## Câu 3 — Tích hợp SSO với AuthService

**Trọng số: 20 điểm**

Toàn bộ hệ sinh thái GDTD đăng nhập tập trung qua **AuthService**. Service của bạn **không** tự quản lý tài khoản, **không** có bảng user/password, **không** tự phát hành token. Nhiệm vụ của backend chỉ là: **nhận token do AuthService cấp, xác minh token đó thật, rồi đọc thông tin người dùng từ nó**.

### 3.1. Thông tin AuthService

AuthService chạy trên **.NET 8 + OpenIddict**. **Bạn không cần cài hay chạy nó** — chúng tôi đã dựng sẵn sandbox, bạn chỉ trỏ cấu hình tới đó.

| Thông số | Giá trị |
|---|---|
| Địa chỉ (Authority) | `https://devhappymath.github.io/auth-sandbox/` |
| Discovery document | `https://devhappymath.github.io/auth-sandbox/.well-known/openid-configuration` |
| `aud` (audience) | `booking-test` |
| Thuật toán ký | **RS256**, token **không mã hóa** → decode đọc được nội dung |

Địa chỉ JWKS **không cần ghi cứng** — bạn cấu hình `Authority`, thư viện tự đọc `jwks_uri` từ discovery document.

Hai đường link trên là **công khai** — mở trình duyệt vào xem được ngay. Hãy mở discovery document trước khi viết code.

**Các claim trong access token:**

| Claim | Ý nghĩa | Ví dụ |
|---|---|---|
| `sub` | Định danh người dùng (GUID) — **đây là `userId` bạn dùng ở Câu 2** | `3fa85f64-5717-4562-b3fc-2c963f66afa6` |
| `email` | Email | `teacher01@test.gdtd.vn` |
| `role` | Vai trò — **có thể xuất hiện nhiều lần** nếu người dùng có nhiều vai trò | `Manager`, `Teacher` |
| `permissions` | Quyền chi tiết — cũng có thể xuất hiện nhiều lần | `Booking.Approve` |
| `aud` | Đối tượng nhận token | `booking-test` |
| `iss` | Nơi phát hành token | `https://devhappymath.github.io/auth-sandbox/` |
| `exp` | Thời điểm hết hạn | (Unix timestamp) |

**Vai trò có trong hệ thống:** `Administrator`, `Manager`, `Teacher`, `Student`, `User`

> [!TIP]
> Hai lỗi hay gặp nhất khi cấu hình lần đầu:
> - `iss` phải **khớp chính xác từng ký tự** với chuỗi trong discovery document, kể cả dấu `/` ở cuối.
> - .NET mặc định **đổi tên một số claim** khi đọc token — `sub` có thể biến thành `nameidentifier`. Nếu `User.FindFirst("sub")` trả `null`, đó là lý do.

#### Token test cấp riêng cho bạn

Email mời làm bài kèm file `tokens.txt` chứa **4 access token** đã phát sẵn, hạn **7 ngày**. Dán thẳng vào header — hoặc vào nút **Authorize** trên Swagger — là chạy được:

```http
GET /api/me
Authorization: Bearer <access_token>
```

| Token | Vai trò | Dùng để test |
|---|---|---|
| `token_teacher1` | `Teacher` | Tạo yêu cầu, hủy yêu cầu của chính mình |
| `token_teacher2` | `Teacher` | Kiểm tra R14 — hủy yêu cầu của người khác phải ra 403 |
| `token_manager` | `Manager` | Duyệt / từ chối (R15), xem tất cả yêu cầu (R16) |
| `token_admin` | `Administrator` | Kiểm tra vai trò quản trị |

> [!NOTE]
> Backend của bạn **không tham gia** vào luồng lấy token — nó chỉ nhận token đã có rồi xác minh. Vì vậy bạn không cần chạy luồng đăng nhập, không cần `client_id`, không cần PKCE.
>
> Token hết hạn giữa chừng thì email xin bộ mới — đây không phải phần chúng tôi muốn thử thách bạn. Và nhớ đừng commit token lên Git.

### 3.2. Phần triển khai

Cấu hình service của bạn để bảo vệ API bằng token từ AuthService:

| # | Yêu cầu |
|---|---|
| 1 | Cấu hình xác thực **JWT Bearer**, lấy khóa công khai từ **JWKS của AuthService** để xác minh chữ ký |
| 2 | Bật kiểm tra đầy đủ: **chữ ký**, **hạn sử dụng** (`exp`), **nơi phát hành** (`iss`), **đối tượng nhận** (`aud`) |
| 3 | Toàn bộ cấu hình (địa chỉ AuthService, audience...) đặt trong `appsettings.json`, **không hardcode** |
| 4 | Viết thành phần đọc thông tin người dùng hiện tại từ token: `userId` (từ `sub`), `email`, danh sách `role`, danh sách `permissions` |
| 5 | Áp dụng vào Câu 2: `userId` khi tạo yêu cầu lấy từ token; phân quyền R14-R16 dựa trên `role` trong token |
| 6 | Tạo endpoint `GET /api/me` trả về thông tin người dùng đang đăng nhập (userId, email, roles, permissions) |
| 7 | Gọi API **không kèm token** → trả **401**; kèm token hợp lệ nhưng **thiếu quyền** → trả **403**. Cả hai đều trả về JSON đúng format chung, **không** trả HTML |

### 3.3. Phần phân tích — Rà soát cấu hình

Một lập trình viên khác trong team đã viết đoạn cấu hình xác thực dưới đây và gửi bạn review. Đoạn code này **biên dịch được và chạy được**, nhưng chứa **nhiều lỗi bảo mật nghiêm trọng**.

```csharp
// ⚠️ ĐOẠN CODE NÀY CỐ TÌNH SAI — dùng để rà soát, đừng copy vào bài làm
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://devhappymath.github.io/auth-sandbox/";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = false,

            SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
        };
    });
```

Và đây là cách người đó lấy thông tin người dùng trong service:

```csharp
public Guid GetCurrentUserId()
{
    var token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"]
        .ToString().Replace("Bearer ", "");

    var jwt = new JwtSecurityToken(token);
    var sub = jwt.Claims.First(c => c.Type == "sub").Value;

    return Guid.Parse(sub);
}
```

**Yêu cầu — trả lời trong `SOLUTION.md`:**

1. Chỉ ra **từng lỗi** trong hai đoạn code trên. Với mỗi lỗi, giải thích ngắn: **kẻ tấn công lợi dụng được điều gì?**
2. Trong các lỗi đó, lỗi nào **nghiêm trọng nhất**? Với lỗi đó, mô tả các bước một kẻ tấn công cần làm để **giả mạo tài khoản Administrator** và tự duyệt yêu cầu đặt phòng của mình.
3. Viết lại **cả hai đoạn** cho đúng.

---

## Hướng dẫn nộp bài

### Cấu trúc bài nộp

```
<HoTen>-backend-test/
├── SOLUTION.md          ← xem nội dung gợi ý bên dưới
├── src/                 ← toàn bộ source code
└── docs/                ← (tùy chọn) sơ đồ ERD, ảnh chụp màn hình Postman/Swagger
```

### Nội dung `SOLUTION.md`

| Mục | Nội dung |
|---|---|
| **1. Hướng dẫn chạy** | Các bước để người khác chạy được project từ máy trắng: dựng database, chạy migration, khởi động API, lấy token test |
| **2. Quyết định thiết kế** | Bạn tổ chức code theo cấu trúc nào và **vì sao**; dùng thư viện gì và **vì sao** |
| **3. Trả lời câu hỏi** | Các câu hỏi ở mục 1.1, 1.2 (API xóa), 2.4, 3.3, 3.4 |
| **4. Phần chưa hoàn thành** | Phần nào chưa làm được và lý do. **Mục này không ảnh hưởng tới điểm** — nó chỉ giúp chúng tôi hiểu đúng bài của bạn |
| **5. Nếu có thêm thời gian** | Bạn sẽ cải thiện điều gì trước tiên? |

### Cách nộp

Nén thư mục thành file `.zip` (**không kèm** `bin/`, `obj/`, `node_modules/`), hoặc đẩy lên một Git repository công khai và gửi link.

> [!TIP]
> Nếu dùng Git, commit thành nhiều lần với message rõ ràng sẽ giúp chúng tôi thấy được cách bạn chia nhỏ công việc — một điểm cộng nhỏ, không có cũng không sao.

### Về việc sử dụng AI

Bạn thoải mái dùng ChatGPT, Claude, GitHub Copilot — đây là công cụ làm việc bình thường của team, chúng tôi cũng dùng hằng ngày.

Chỉ có một lưu ý: buổi phỏng vấn sau đó sẽ xoay quanh chính code bạn nộp, nên hãy chọn cách viết mà bạn thấy mình giải thích được. Code đơn giản mà bạn nắm chắc luôn tốt hơn code phức tạp mà bạn chưa kịp hiểu.

### Liên hệ

Có điểm nào chưa rõ trong đề bài, cứ nhắn hỏi chúng tôi — đặt câu hỏi làm rõ yêu cầu là điều chúng tôi mong đợi, hoàn toàn không phải điểm trừ.
