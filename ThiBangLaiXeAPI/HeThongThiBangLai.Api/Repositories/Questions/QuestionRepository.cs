using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.Data;
using HeThongThiBangLai.Api.Models;
using HeThongThiBangLai.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiBangLai.Api.Repositories.Questions;

public class QuestionRepository : IQuestionRepository
{
    private readonly ApplicationDbContext _context;

    public QuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<cau_hoi?> GetByIdAsync(long id)
    {
        return await _context.cau_hois.FindAsync(id);
    }

    public async Task<List<cau_hoi>> GetAllAsync()
    {
        return await _context.cau_hois.ToListAsync();
    }

    public async Task<PagedList<cau_hoi>> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _context.cau_hois.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(q => q.noi_dung.Contains(search));
        }

        return await PagedList<cau_hoi>.CreateAsync(query, page, pageSize);
    }

    public async Task<PagedList<cau_hoi>> GetPagedWithAnswersAsync(int page, int pageSize, string? search = null, long? topicId = null, string? topicCode = null, string? status = null, bool? isCritical = null, bool includeCorrectAnswer = false)
    {
        var query = _context.cau_hois
            .AsNoTracking()
            .Include(question => question.chu_de)
            .Include(question => question.dap_ans)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(question => question.noi_dung.Contains(keyword));
        }

        if (topicId.HasValue)
        {
            query = query.Where(question => question.chu_de_id == topicId.Value);
        }

        if (!string.IsNullOrWhiteSpace(topicCode))
        {
            var normalizedTopicCode = topicCode.Trim().ToUpperInvariant();
            query = query.Where(question => question.chu_de.ma_chu_de == normalizedTopicCode);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(question => question.trang_thai == normalizedStatus);
        }

        if (isCritical.HasValue)
        {
            query = query.Where(question => question.la_cau_diem_liet == isCritical.Value);
        }

        query = query.OrderBy(question => question.chu_de_id).ThenBy(question => question.id);

        return await PagedList<cau_hoi>.CreateAsync(query, page, pageSize);
    }

    public async Task AddAsync(cau_hoi question)
    {
        await _context.cau_hois.AddAsync(question);
    }

    public void Update(cau_hoi question)
    {
        _context.cau_hois.Update(question);
    }

    public void Remove(cau_hoi question)
    {
        _context.cau_hois.Remove(question);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
