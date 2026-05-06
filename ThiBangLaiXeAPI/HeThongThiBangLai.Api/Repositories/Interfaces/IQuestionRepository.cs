using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.Models;

namespace HeThongThiBangLai.Api.Repositories.Interfaces;

public interface IQuestionRepository
{
    Task<cau_hoi?> GetByIdAsync(long id);
    Task<List<cau_hoi>> GetAllAsync();
    Task<PagedList<cau_hoi>> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<PagedList<cau_hoi>> GetPagedWithAnswersAsync(int page, int pageSize, string? search = null, long? topicId = null, string? topicCode = null, string? status = null, bool? isCritical = null, bool includeCorrectAnswer = false);
    Task AddAsync(cau_hoi question);
    void Update(cau_hoi question);
    void Remove(cau_hoi question);
    Task SaveChangesAsync();
}
