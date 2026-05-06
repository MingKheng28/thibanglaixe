using HeThongThiBangLai.Api.Common.Exceptions;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.WrongQuestions;
using HeThongThiBangLai.Api.Models;
using HeThongThiBangLai.Api.Repositories.Interfaces;
using HeThongThiBangLai.Api.Services.Interfaces;

namespace HeThongThiBangLai.Api.Services.WrongQuestions;

public class WrongQuestionService : IWrongQuestionService
{
    private readonly IWrongQuestionRepository _repository;

    public WrongQuestionService(IWrongQuestionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<WrongPracticeSessionDto>> CreatePracticeSessionAsync(long userId, CreateWrongPracticeSessionRequestDto request)
    {
        var student = await _repository.GetOrCreateStudentByUserIdAsync(userId);

        var questionIds = request.QuestionIds
            .Distinct()
            .ToList();

        var stats = await _repository.GetWrongQuestionStatsAsync(student.id);
        var handledQuestionIds = await _repository.GetHandledQuestionIdsAsync(userId);

        var unresolvedQuestionIds = stats
            .Where(x => !handledQuestionIds.Contains(x.QuestionId))
            .Select(x => x.QuestionId)
            .ToHashSet();

        var invalidQuestionIds = questionIds
            .Where(questionId => !unresolvedQuestionIds.Contains(questionId))
            .ToList();

        if (invalidQuestionIds.Count != 0)
        {
            throw new BusinessRuleAppException("Some questions are not in the unresolved wrong-question pool", "INVALID_WRONG_QUESTION_SELECTION");
        }

        var questions = await _repository.GetQuestionsByIdsAsync(questionIds);
        if (questions.Count != questionIds.Count)
        {
            throw new NotFoundAppException("One or more questions were not found or are not approved");
        }

        var orderedQuestions = questionIds
            .Join(questions, id => id, question => question.id, (_, question) => question)
            .ToList();

        return await CreatePracticeSessionInternalAsync(userId, student.id, orderedQuestions);
    }

    public async Task<ApiResponse<List<WrongQuestionDto>>> GetListAsync(long userId)
    {
        var student = await _repository.GetOrCreateStudentByUserIdAsync(userId);

        var stats = await _repository.GetWrongQuestionStatsAsync(student.id);
        var handledQuestionIds = await _repository.GetHandledQuestionIdsAsync(userId);

        var unresolvedStats = stats
            .Where(x => !handledQuestionIds.Contains(x.QuestionId))
            .ToList();

        var questions = await _repository.GetQuestionsByIdsAsync(unresolvedStats.Select(x => x.QuestionId));
        var statByQuestionId = unresolvedStats.ToDictionary(x => x.QuestionId, x => x);

        var result = questions
            .Select(x => new WrongQuestionDto
            {
                QuestionId = x.id,
                TopicId = x.chu_de_id,
                Content = x.noi_dung,
                Level = x.muc_do,
                WrongCount = statByQuestionId.TryGetValue(x.id, out var stat) ? stat.WrongCount : 0
            })
            .OrderByDescending(x => x.WrongCount)
            .ThenBy(x => x.QuestionId)
            .ToList();

        return ApiResponseFactory.Success(result, "Wrong questions retrieved successfully");
    }

    public async Task<ApiResponse<WrongQuestionSummaryDto>> GetSummaryAsync(long userId)
    {
        var student = await _repository.GetOrCreateStudentByUserIdAsync(userId);

        var stats = await _repository.GetWrongQuestionStatsAsync(student.id);
        var totalWrongQuestions = stats.Count;

        var handledQuestionIds = await _repository.GetHandledQuestionIdsAsync(userId);
        var resolvedQuestions = stats.Count(x => handledQuestionIds.Contains(x.QuestionId));
        var unresolvedQuestions = totalWrongQuestions - resolvedQuestions;

        var practiceSessions = await _repository.GetWrongPracticeSessionCountAsync(student.id);
        var latestPracticeAt = await _repository.GetLatestWrongPracticeAtAsync(student.id);

        var summary = new WrongQuestionSummaryDto
        {
            TotalWrongQuestions = totalWrongQuestions,
            UnresolvedQuestions = unresolvedQuestions,
            ResolvedQuestions = resolvedQuestions,
            TotalPracticeSessions = practiceSessions,
            LatestPracticeAt = latestPracticeAt
        };

        return ApiResponseFactory.Success(summary, "Wrong question summary retrieved successfully");
    }

    public async Task<ApiResponse<WrongPracticeSessionDto>> StartPracticeAsync(long userId, StartWrongPracticeRequestDto request)
    {
        var student = await _repository.GetOrCreateStudentByUserIdAsync(userId);

        var stats = await _repository.GetWrongQuestionStatsAsync(student.id);
        var handledQuestionIds = await _repository.GetHandledQuestionIdsAsync(userId);

        var unresolvedQuestionIds = stats
            .Where(x => !handledQuestionIds.Contains(x.QuestionId))
            .Select(x => x.QuestionId)
            .ToList();

        var questions = await _repository.GetQuestionsByIdsAsync(unresolvedQuestionIds);
        if (questions.Count < request.Size)
        {
            throw new BusinessRuleAppException("Not enough unresolved wrong questions for practice", "NOT_ENOUGH_WRONG_QUESTIONS");
        }

        var selectedQuestions = questions
            .OrderBy(_ => Guid.NewGuid())
            .Take(request.Size)
            .ToList();

        return await CreatePracticeSessionInternalAsync(userId, student.id, selectedQuestions);
    }

    private async Task<ApiResponse<WrongPracticeSessionDto>> CreatePracticeSessionInternalAsync(long userId, long studentId, List<cau_hoi> selectedQuestions)
    {
        var now = DateTime.UtcNow;

        var session = new phien_on_tap
        {
            hoc_vien_id = studentId,
            ngay_tao = now,
            thoi_gian_bat_dau = now,
            tong_so_cau = selectedQuestions.Count,
            so_cau_dung = 0,
            diem = 0,
            trang_thai = "wrong_practice_started"
        };

        await _repository.AddPracticeSessionAsync(session);
        await _repository.SaveChangesAsync();

        var sessionQuestions = selectedQuestions
            .Select((question, index) => new phien_on_tap_cau_hoi
            {
                phien_on_tap_id = session.id,
                cau_hoi_id = question.id,
                thu_tu_cau = index + 1
            })
            .ToList();

        await _repository.AddPracticeSessionQuestionsAsync(sessionQuestions);

        await _repository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = userId,
            hanh_dong = "wrong_practice_started",
            bang_tac_dong = "phien_on_tap",
            khoa_chinh_du_lieu = session.id,
            noi_dung = $"Started wrong practice session with {selectedQuestions.Count} questions",
            created_at = now
        });

        await _repository.SaveChangesAsync();

        var result = new WrongPracticeSessionDto
        {
            SessionId = session.id,
            TotalQuestions = selectedQuestions.Count,
            StartedAt = session.thoi_gian_bat_dau ?? now,
            Status = session.trang_thai,
            QuestionIds = selectedQuestions.Select(x => x.id).ToList()
        };

        return ApiResponseFactory.Created(result, "Wrong question practice started successfully");
    }

    public async Task<ApiResponse<object>> ResolveAsync(long userId, long questionId)
    {
        await EnsureQuestionIsInWrongPoolAsync(userId, questionId);

        var log = new nhat_ky_he_thong
        {
            nguoi_dung_id = userId,
            hanh_dong = "wrong_question_resolved",
            bang_tac_dong = "wrong_question",
            khoa_chinh_du_lieu = questionId,
            noi_dung = "Marked wrong question as resolved",
            created_at = DateTime.UtcNow
        };

        await _repository.AddSystemLogAsync(log);
        await _repository.SaveChangesAsync();

        return ApiResponseFactory.Success<object>(new { questionId }, "Wrong question marked as resolved");
    }

    public async Task<ApiResponse<object>> DeleteAsync(long userId, long questionId)
    {
        await EnsureQuestionIsInWrongPoolAsync(userId, questionId);

        var log = new nhat_ky_he_thong
        {
            nguoi_dung_id = userId,
            hanh_dong = "wrong_question_removed",
            bang_tac_dong = "wrong_question",
            khoa_chinh_du_lieu = questionId,
            noi_dung = "Removed wrong question from practice list",
            created_at = DateTime.UtcNow
        };

        await _repository.AddSystemLogAsync(log);
        await _repository.SaveChangesAsync();

        return ApiResponseFactory.Success<object>(new { questionId }, "Wrong question removed successfully");
    }

    private async Task EnsureQuestionIsInWrongPoolAsync(long userId, long questionId)
    {
        var student = await _repository.GetOrCreateStudentByUserIdAsync(userId);

        var stats = await _repository.GetWrongQuestionStatsAsync(student.id);
        var existsInWrongPool = stats.Any(x => x.QuestionId == questionId);
        if (!existsInWrongPool)
        {
            throw new NotFoundAppException("Wrong question not found");
        }
    }
}
