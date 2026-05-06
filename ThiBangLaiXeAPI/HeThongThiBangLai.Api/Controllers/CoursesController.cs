using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.Courses;
using HeThongThiBangLai.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeThongThiBangLai.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet("courses")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedList<CourseListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? status = null)
    {
        var result = await _courseService.GetCoursesAsync(page, pageSize, search, status);
        return Ok(result);
    }

    [HttpGet("courses/{courseId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CourseDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseById(long courseId)
    {
        var result = await _courseService.GetCourseByIdAsync(courseId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("courses/{courseId:long}/classes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CourseClassDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseClasses(long courseId)
    {
        var result = await _courseService.GetCourseClassesAsync(courseId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("course-registrations")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CourseRegistrationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterCourse([FromBody] CreateCourseRegistrationRequestDto request)
    {
        var result = await _courseService.RegisterCourseAsync(request, GetCurrentUserId());
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("course-registrations/{registrationId:long}/approve")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CourseRegistrationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveCourseRegistration(long registrationId, [FromBody] ApproveCourseRegistrationRequestDto request)
    {
        var result = await _courseService.ApproveRegistrationAsync(registrationId, request, GetCurrentUserId());
        return Ok(result);
    }

    [HttpGet("my/course-registrations")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedList<MyCourseRegistrationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCourseRegistrations([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _courseService.GetMyRegistrationsAsync(GetCurrentUserId(), page, pageSize);
        return Ok(result);
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin người dùng.");
        }

        return userId;
    }
}
