using HeThongThiBangLai.Api.Models;

namespace HeThongThiBangLai.Api.Repositories.Interfaces;

public interface ICriticalQuestionRepository
{
    Task<List<cau_hoi>> GetCriticalQuestionsAsync();
    Task<hoc_vien> GetOrCreateStudentByUserIdAsync(long userId);
    Task<int> GetCriticalPracticeSessionCountAsync(long hocVienId);
    Task<DateTime?> GetLatestCriticalPracticeAtAsync(long hocVienId);
    Task AddPracticeSessionAsync(phien_on_tap session);
    Task AddPracticeSessionQuestionsAsync(IEnumerable<phien_on_tap_cau_hoi> questions);
    Task SaveChangesAsync();
}
