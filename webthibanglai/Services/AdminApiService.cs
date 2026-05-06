using System.Net.Http.Headers;
using System.Text.Json;
using webthibanglai.Models;

namespace webthibanglai.Services;

public interface IAdminApiService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(string? accessToken, string adminName, string adminEmail, CancellationToken cancellationToken = default);
    Task<string> SendAdminRequestAsync(string accessToken, HttpMethod method, string endpoint, string? body, CancellationToken cancellationToken = default);
}

public sealed class AdminApiService : IAdminApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdminApiService> _logger;

    public AdminApiService(IHttpClientFactory httpClientFactory, ILogger<AdminApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(string? accessToken, string adminName, string adminEmail, CancellationToken cancellationToken = default)
    {
        var model = new AdminDashboardViewModel
        {
            AdminName = string.IsNullOrWhiteSpace(adminName) ? "Admin" : adminName,
            AdminEmail = adminEmail ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            model.ErrorMessage = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            return model;
        }

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var usersTask = GetListAsync<AdminUserItem>(client, "/api/v1/admin/users", cancellationToken);
            var coursesTask = GetListAsync<AdminCourseItem>(client, "/api/v1/admin/courses", cancellationToken);
            var classesTask = GetListAsync<AdminClassItem>(client, "/api/v1/admin/classes", cancellationToken);
            var schedulesTask = GetListAsync<AdminScheduleItem>(client, "/api/v1/admin/schedules", cancellationToken);
            var questionsTask = GetListAsync<AdminQuestionItem>(client, "/api/v1/admin/questions", cancellationToken);
            var examsTask = GetListAsync<AdminExamItem>(client, "/api/v1/admin/exams", cancellationToken);
            var receiptsTask = GetListAsync<AdminReceiptItem>(client, "/api/v1/admin/receipts", cancellationToken);

            await Task.WhenAll(usersTask, coursesTask, classesTask, schedulesTask, questionsTask, examsTask, receiptsTask);

            var users = usersTask.Result;
            var courses = coursesTask.Result;
            var classes = classesTask.Result;
            var schedules = schedulesTask.Result;
            var questions = questionsTask.Result;
            var exams = examsTask.Result;
            var receipts = receiptsTask.Result;
            var today = DateOnly.FromDateTime(DateTime.Now);

            model.TotalUsers = users.Count;
            model.ActiveUsers = users.Count(item => IsActive(item.TrangThai));
            model.TotalCourses = courses.Count;
            model.OpenCourses = courses.Count(item => IsActive(item.TrangThai) || item.TrangThai.Contains("mo", StringComparison.OrdinalIgnoreCase));
            model.TotalClasses = classes.Count;
            model.TotalSchedules = schedules.Count;
            model.TotalQuestions = questions.Count;
            model.CriticalQuestions = questions.Count(item => item.LaCauDiemLiet);
            model.TotalExams = exams.Count;
            model.TotalReceipts = receipts.Count;
            model.TotalReceiptAmount = receipts.Sum(item => item.TongTien);
            model.PendingReceipts = receipts.Count(item => item.TrangThai.Contains("cho", StringComparison.OrdinalIgnoreCase));
            model.RecentUsers = users.OrderByDescending(item => item.CreatedAt ?? DateTime.MinValue).Take(6).ToList();
            model.Courses = courses.OrderBy(item => item.TenKhoaHoc).Take(8).ToList();
            model.TodaySchedules = schedules
                .Where(item => item.NgayHoc == today)
                .OrderBy(item => item.GioBatDau)
                .Take(6)
                .ToList();
            model.RecentExams = exams.OrderByDescending(item => item.NgayTao ?? DateTime.MinValue).Take(6).ToList();
            model.RecentReceipts = receipts.OrderByDescending(item => item.NgayThu ?? DateTime.MinValue).Take(6).ToList();

            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading admin dashboard data.");
            model.ErrorMessage = "Không tải được dữ liệu quản trị từ API. Vui lòng kiểm tra backend và quyền truy cập admin.";
            return model;
        }
    }

    public async Task<string> SendAdminRequestAsync(string accessToken, HttpMethod method, string endpoint, string? body, CancellationToken cancellationToken = default)
    {
        if (!endpoint.StartsWith("/api/v1/admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only admin API endpoints can be proxied.");
        }

        var client = _httpClientFactory.CreateClient("ApiClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var request = new HttpRequestMessage(method, endpoint);
        if (!string.IsNullOrWhiteSpace(body) && method != HttpMethod.Get && method != HttpMethod.Delete)
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<List<T>> GetListAsync<T>(HttpClient client, string endpoint, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(endpoint, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Admin API request failed. Endpoint={Endpoint}, StatusCode={StatusCode}, Response={Response}", endpoint, response.StatusCode, body);
            return new List<T>();
        }

        var envelope = JsonSerializer.Deserialize<AdminApiEnvelope<List<T>>>(body, JsonOptions());
        return envelope?.Data ?? new List<T>();
    }

    private static bool IsActive(string? status)
    {
        return string.Equals(status, "hoat_dong", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "dang_mo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }
}
