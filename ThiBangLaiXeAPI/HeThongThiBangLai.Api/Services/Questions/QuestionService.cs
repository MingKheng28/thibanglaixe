using AutoMapper;
using HeThongThiBangLai.Api.Common.Exceptions;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.Questions;
using HeThongThiBangLai.Api.Models;
using HeThongThiBangLai.Api.Repositories.Interfaces;
using HeThongThiBangLai.Api.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace HeThongThiBangLai.Api.Services.Questions;

public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _repository;
    private readonly IMapper _mapper;
    private readonly string _assetsBaseUrl;
    private readonly HashSet<string> _availableAssetFiles;

    public QuestionService(IQuestionRepository repository, IMapper mapper, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _repository = repository;
        _mapper = mapper;

        var configuredAssetsBaseUrl = configuration["Assets:BaseUrl"]?.Trim();
        _assetsBaseUrl = string.IsNullOrWhiteSpace(configuredAssetsBaseUrl)
            ? "/assets"
            : configuredAssetsBaseUrl.TrimEnd('/');

        var assetsDirectory = Path.Combine(environment.WebRootPath ?? string.Empty, "assets");
        _availableAssetFiles = Directory.Exists(assetsDirectory)
            ? Directory.EnumerateFiles(assetsDirectory)
                .Select(Path.GetFileName)
                .Where(static fileName => !string.IsNullOrWhiteSpace(fileName))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ApiResponse<QuestionDto>> GetByIdAsync(long id)
    {
        var question = await _repository.GetByIdAsync(id);
        if (question == null)
            return ApiResponseFactory.Fail<QuestionDto>("Question not found");

        var dto = _mapper.Map<QuestionDto>(question);
        return ApiResponseFactory.Success(dto, "Question retrieved successfully");
    }

    public async Task<ApiResponse<PagedList<QuestionListResponseDto>>> GetListAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        var pagedQuestions = await _repository.GetPagedAsync(page, pageSize, search);
        var dtos = _mapper.Map<List<QuestionListResponseDto>>(pagedQuestions.Items);

        var pagedDtos = new PagedList<QuestionListResponseDto>(dtos, pagedQuestions.TotalCount, page, pageSize);

        return ApiResponseFactory.SuccessPaged(pagedDtos, "Questions retrieved successfully");
    }

    public async Task<ApiResponse<PagedList<QuestionWithAnswersDto>>> GetListWithAnswersAsync(int page = 1, int pageSize = 20, string? search = null, long? topicId = null, string? topicCode = null, string? status = null, bool? isCritical = null, bool includeCorrectAnswer = false, bool includeExplanation = false)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        var pagedQuestions = await _repository.GetPagedWithAnswersAsync(page, pageSize, search, topicId, topicCode, status, isCritical, includeCorrectAnswer);
        var dtos = pagedQuestions.Items.Select(question => MapWithAnswers(question, includeCorrectAnswer, includeExplanation, _assetsBaseUrl, _availableAssetFiles)).ToList();
        var pagedDtos = new PagedList<QuestionWithAnswersDto>(dtos, pagedQuestions.TotalCount, page, pageSize);

        return ApiResponseFactory.SuccessPaged(pagedDtos, "Questions with answers retrieved successfully");
    }

    public async Task<ApiResponse<QuestionDto>> CreateAsync(CreateQuestionRequestDto request)
    {
        var question = _mapper.Map<cau_hoi>(request);
        question.trang_thai = "draft";

        await _repository.AddAsync(question);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<QuestionDto>(question);
        return ApiResponseFactory.Created(dto, "Question created successfully");
    }

    public async Task<ApiResponse<QuestionDto>> UpdateAsync(long id, UpdateQuestionRequestDto request)
    {
        var question = await _repository.GetByIdAsync(id);
        if (question == null)
        {
            throw new NotFoundAppException("Question not found");
        }

        _mapper.Map(request, question);
        _repository.Update(question);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<QuestionDto>(question);
        return ApiResponseFactory.Success(dto, "Question updated successfully");
    }

    public async Task<ApiResponse<QuestionDto>> ApproveAsync(long id)
    {
        var question = await _repository.GetByIdAsync(id);
        if (question == null)
        {
            throw new NotFoundAppException("Question not found");
        }

        question.trang_thai = "approved";
        _repository.Update(question);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<QuestionDto>(question);
        return ApiResponseFactory.Success(dto, "Question approved successfully");
    }

    public async Task<ApiResponse<QuestionDto>> ArchiveAsync(long id)
    {
        var question = await _repository.GetByIdAsync(id);
        if (question == null)
        {
            throw new NotFoundAppException("Question not found");
        }

        question.trang_thai = "archived";
        _repository.Update(question);
        await _repository.SaveChangesAsync();

        var dto = _mapper.Map<QuestionDto>(question);
        return ApiResponseFactory.Success(dto, "Question archived successfully");
    }

    public async Task DeleteAsync(long id)
    {
        var question = await _repository.GetByIdAsync(id);
        if (question == null)
        {
            throw new NotFoundAppException("Question not found");
        }

        _repository.Remove(question);
        await _repository.SaveChangesAsync();
    }
    private static QuestionWithAnswersDto MapWithAnswers(cau_hoi question, bool includeCorrectAnswer, bool includeExplanation, string? assetsBaseUrl, HashSet<string> availableAssetFiles)
    {
        var imageUrl = BuildQuestionImageUrl(question.id, assetsBaseUrl, availableAssetFiles);

        return new QuestionWithAnswersDto
        {
            Id = question.id,
            TopicId = question.chu_de_id,
            TopicCode = question.chu_de.ma_chu_de,
            TopicName = question.chu_de.ten_chu_de,
            Content = question.noi_dung,
            QuestionType = question.loai_cau_hoi,
            Level = question.muc_do,
            IsCritical = question.la_cau_diem_liet,
            Status = question.trang_thai,
            Explanation = includeExplanation ? question.giai_thich_dap_an : null,
            ImageUrl = imageUrl,
            Answers = question.dap_ans
                .OrderBy(answer => answer.thu_tu)
                .Select(answer => new QuestionAnswerOptionDto
                {
                    AnswerId = answer.id,
                    Content = answer.noi_dung,
                    Order = answer.thu_tu,
                    IsCorrect = includeCorrectAnswer ? answer.la_dap_an_dung : null
                })
                .ToList()
        };
    }

    private static string? BuildQuestionImageUrl(long questionId, string assetsBaseUrl, HashSet<string> availableAssetFiles)
    {
        var extension = questionId is >= 212 and <= 215 ? "png" : "jpg";
        var fileName = $"{questionId}.{extension}";

        return availableAssetFiles.Contains(fileName)
            ? $"{assetsBaseUrl}/{fileName}"
            : null;
    }
}
