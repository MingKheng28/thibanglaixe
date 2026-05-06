using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.Courses;

namespace HeThongThiBangLai.Api.Services.Interfaces;

public interface ICourseService
{
    Task<ApiResponse<PagedList<CourseListItemDto>>> GetCoursesAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null);
    Task<ApiResponse<CourseDetailDto>> GetCourseByIdAsync(long courseId);
    Task<ApiResponse<List<CourseClassDto>>> GetCourseClassesAsync(long courseId);
    Task<ApiResponse<CourseRegistrationDto>> RegisterCourseAsync(CreateCourseRegistrationRequestDto request, long currentUserId);
    Task<ApiResponse<CourseRegistrationDto>> ApproveRegistrationAsync(long registrationId, ApproveCourseRegistrationRequestDto request, long approverUserId);
    Task<ApiResponse<PagedList<MyCourseRegistrationDto>>> GetMyRegistrationsAsync(long currentUserId, int page = 1, int pageSize = 10);
}
