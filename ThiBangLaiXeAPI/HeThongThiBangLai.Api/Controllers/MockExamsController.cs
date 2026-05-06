using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.ExamSessions;
using HeThongThiBangLai.Api.DTOs.Exams;
using HeThongThiBangLai.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeThongThiBangLai.Api.Controllers;

[ApiController]
[Route("api/v1/mock-exams")]
[Produces("application/json")]
public class MockExamsController : ControllerBase
{
    private readonly ISampleExamService _sampleExamService;
    private readonly IExamSessionService _examSessionService;

    public MockExamsController(ISampleExamService sampleExamService, IExamSessionService examSessionService)
    {
        _sampleExamService = sampleExamService;
        _examSessionService = examSessionService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedList<SampleExamDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _sampleExamService.GetPublishedListAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SampleExamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _sampleExamService.GetPublishedByIdAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:long}/start")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<StartExamSessionResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Start(long id)
    {
        var result = await _examSessionService.StartSampleExamAsync(GetCurrentUserId(), id);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("sessions/{sessionId:long}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ExamSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(long sessionId)
    {
        var result = await _examSessionService.GetSessionAsync(GetCurrentUserId(), sessionId);
        return Ok(result);
    }

    [HttpGet("sessions/{sessionId:long}/questions/{number:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ExamSessionQuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestion(long sessionId, int number)
    {
        var result = await _examSessionService.GetQuestionAsync(GetCurrentUserId(), sessionId, number);
        return Ok(result);
    }

    [HttpPost("sessions/{sessionId:long}/answers")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAnswer(long sessionId, [FromBody] SubmitExamAnswerRequestDto request)
    {
        var result = await _examSessionService.SubmitAnswerAsync(GetCurrentUserId(), sessionId, request);
        return Ok(result);
    }

    [HttpPost("sessions/{sessionId:long}/submit")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ExamSessionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(long sessionId)
    {
        var result = await _examSessionService.SubmitAsync(GetCurrentUserId(), sessionId);
        return Ok(result);
    }

    [HttpGet("sessions/{sessionId:long}/result")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ExamSessionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(long sessionId)
    {
        var result = await _examSessionService.GetResultAsync(GetCurrentUserId(), sessionId);
        return Ok(result);
    }

    [HttpGet("sessions/{sessionId:long}/review")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ExamSessionReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReview(long sessionId)
    {
        var result = await _examSessionService.GetReviewAsync(GetCurrentUserId(), sessionId);
        return Ok(result);
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin người dùng.");

        return userId;
    }
}
