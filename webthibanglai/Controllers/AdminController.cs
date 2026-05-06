using Microsoft.AspNetCore.Mvc;
using webthibanglai.Services;

namespace webthibanglai.Controllers;

public sealed class AdminController : Controller
{
    private const string AccessTokenSessionKey = "AccessToken";
    private readonly IAdminApiService _adminApiService;

    public AdminController(IAdminApiService adminApiService)
    {
        _adminApiService = adminApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString(AccessTokenSessionKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["LoginSuccess"] = "Vui lòng đăng nhập bằng tài khoản quản trị để vào Admin.";
            return RedirectToAction("Index", "Login", new { returnUrl = Url.Action(nameof(Index), "Admin") });
        }

        ViewBag.HideChatbot = true;
        ViewBag.AdminAccessToken = token;
        var adminName = TempData.Peek("AuthUsername")?.ToString() ?? "Admin";
        var adminEmail = TempData.Peek("AuthEmail")?.ToString() ?? string.Empty;
        var model = await _adminApiService.GetDashboardAsync(token, adminName, adminEmail, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ApiProxy([FromQuery] string path, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString(AccessTokenSessionKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
        }

        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/api/v1/admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Đường dẫn API quản trị không hợp lệ." });
        }

        var payload = await _adminApiService.SendAdminRequestAsync(token, HttpMethod.Get, path, null, cancellationToken);
        return Content(payload, "application/json");
    }

    [ActionName(nameof(ApiProxy))]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    public async Task<IActionResult> ApiProxyWrite([FromQuery] string path, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString(AccessTokenSessionKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
        }

        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/api/v1/admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Đường dẫn API quản trị không hợp lệ." });
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var method = new HttpMethod(Request.Method);
        var payload = await _adminApiService.SendAdminRequestAsync(token, method, path, body, cancellationToken);
        return Content(payload, "application/json");
    }
}
