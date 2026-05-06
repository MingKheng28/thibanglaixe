using HeThongThiBangLai.Api.Models;

namespace HeThongThiBangLai.Api.Repositories.Interfaces;

public interface IExamSessionRepository
{
    Task<hoc_vien> GetOrCreateStudentByUserIdAsync(long userId);
    Task<bool> UserExistsAsync(long userId);
    Task<de_thi?> GetPublishedSampleExamByIdAsync(long sampleExamId);
    Task<long> GetOrCreateSampleExamSlotIdAsync(long examPeriodId);

    Task AddExamSessionAsync(bai_thi session);
    Task AddExamSessionDetailsAsync(IEnumerable<chi_tiet_bai_thi> details);
    void UpdateExamSession(bai_thi session);
    void UpdateExamSessionDetail(chi_tiet_bai_thi detail);

    Task<bai_thi?> GetSessionByIdForUserAsync(long sessionId, long userId);
    Task<List<chi_tiet_bai_thi>> GetSessionDetailsAsync(long sessionId);
    Task<Dictionary<long, string>> GetPrimaryQuestionImageUrlsAsync(IEnumerable<long> questionIds);

    Task AddSystemLogAsync(nhat_ky_he_thong log);
    Task SaveChangesAsync();
}
