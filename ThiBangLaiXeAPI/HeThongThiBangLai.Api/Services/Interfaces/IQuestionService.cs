using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.Questions;

namespace HeThongThiBangLai.Api.Services.Interfaces;

public interface IQuestionService
{
    Task<ApiResponse<QuestionDto>> GetByIdAsync(long id);
    Task<ApiResponse<PagedList<QuestionListResponseDto>>> GetListAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<ApiResponse<PagedList<QuestionWithAnswersDto>>> GetListWithAnswersAsync(int page = 1, int pageSize = 20, string? search = null, long? topicId = null, string? topicCode = null, string? status = null, bool? isCritical = null, bool includeCorrectAnswer = false, bool includeExplanation = false);
    Task<ApiResponse<QuestionDto>> CreateAsync(CreateQuestionRequestDto request);
    Task<ApiResponse<QuestionDto>> UpdateAsync(long id, UpdateQuestionRequestDto request);
    Task<ApiResponse<QuestionDto>> ApproveAsync(long id);
    Task<ApiResponse<QuestionDto>> ArchiveAsync(long id);
    Task DeleteAsync(long id);
}
