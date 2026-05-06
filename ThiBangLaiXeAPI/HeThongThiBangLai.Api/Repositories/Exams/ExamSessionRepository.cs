using HeThongThiBangLai.Api.Data;
using HeThongThiBangLai.Api.Models;
using HeThongThiBangLai.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiBangLai.Api.Repositories.Exams;

public class ExamSessionRepository : IExamSessionRepository
{
    private readonly ApplicationDbContext _context;

    public ExamSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<hoc_vien> GetOrCreateStudentByUserIdAsync(long userId)
    {
        var student = await _context.hoc_viens.FirstOrDefaultAsync(x => x.nguoi_dung_id == userId);
        if (student is not null)
        {
            return student;
        }

        var user = await _context.nguoi_dungs.FirstOrDefaultAsync(x => x.id == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        student = new hoc_vien
        {
            nguoi_dung_id = userId,
            ho_ten = user.ten_dang_nhap,
            created_at = DateTime.UtcNow
        };

        await _context.hoc_viens.AddAsync(student);
        await _context.SaveChangesAsync();

        return student;
    }

    public async Task<bool> UserExistsAsync(long userId)
    {
        return await _context.nguoi_dungs.AnyAsync(x => x.id == userId);
    }

    public async Task<de_thi?> GetPublishedSampleExamByIdAsync(long sampleExamId)
    {
        return await _context.de_this
            .Include(x => x.de_thi_cau_hois)
                .ThenInclude(x => x.cau_hoi)
                    .ThenInclude(x => x.dap_ans)
            .FirstOrDefaultAsync(x => x.id == sampleExamId && x.trang_thai == "published");
    }

    public async Task<long> GetOrCreateSampleExamSlotIdAsync(long examPeriodId)
    {
        var existingSlot = await _context.ca_this
            .OrderBy(x => x.id)
            .FirstOrDefaultAsync(x => x.ky_thi_id == examPeriodId);

        if (existingSlot is not null)
        {
            return existingSlot.id;
        }

        var slot = new ca_thi
        {
            ky_thi_id = examPeriodId,
            ma_ca_thi = $"MOCK_{examPeriodId}",
            ten_ca_thi = "Ca thi thử mô phỏng",
            gio_bat_dau = new TimeOnly(0, 0),
            gio_ket_thuc = new TimeOnly(23, 59),
            phong_thi = "Online",
            so_luong_toi_da = 999999
        };

        await _context.ca_this.AddAsync(slot);
        await _context.SaveChangesAsync();

        return slot.id;
    }

    public async Task AddExamSessionAsync(bai_thi session)
    {
        await _context.bai_this.AddAsync(session);
    }

    public async Task AddExamSessionDetailsAsync(IEnumerable<chi_tiet_bai_thi> details)
    {
        await _context.chi_tiet_bai_this.AddRangeAsync(details);
    }

    public void UpdateExamSession(bai_thi session)
    {
        _context.bai_this.Update(session);
    }

    public void UpdateExamSessionDetail(chi_tiet_bai_thi detail)
    {
        _context.chi_tiet_bai_this.Update(detail);
    }

    public async Task<bai_thi?> GetSessionByIdForUserAsync(long sessionId, long userId)
    {
        return await _context.bai_this
            .Include(x => x.de_thi)
            .Include(x => x.chi_tiet_bai_this)
                .ThenInclude(x => x.cau_hoi)
                    .ThenInclude(x => x.dap_ans)
            .Include(x => x.chi_tiet_bai_this)
                .ThenInclude(x => x.dap_an_chon)
            .FirstOrDefaultAsync(x => x.id == sessionId && x.nguoi_dung_id == userId);
    }

    public async Task<List<chi_tiet_bai_thi>> GetSessionDetailsAsync(long sessionId)
    {
        return await _context.chi_tiet_bai_this
            .Include(x => x.cau_hoi)
                .ThenInclude(x => x.dap_ans)
            .Include(x => x.dap_an_chon)
            .Where(x => x.bai_thi_id == sessionId)
            .OrderBy(x => x.id)
            .ToListAsync();
    }

    public async Task<Dictionary<long, string>> GetPrimaryQuestionImageUrlsAsync(IEnumerable<long> questionIds)
    {
        var ids = questionIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        return await _context.file_usages
            .Include(x => x.file)
            .Where(x => x.entity_name == "cau_hoi"
                && x.field_name == "question_image"
                && x.is_primary
                && ids.Contains(x.entity_id)
                && x.file.trang_thai == "active")
            .GroupBy(x => x.entity_id)
            .Select(g => new
            {
                QuestionId = g.Key,
                ImageUrl = g.OrderBy(x => x.sort_order).ThenBy(x => x.id).Select(x => x.file.public_url).First()
            })
            .ToDictionaryAsync(x => x.QuestionId, x => x.ImageUrl);
    }

    public async Task AddSystemLogAsync(nhat_ky_he_thong log)
    {
        await _context.nhat_ky_he_thongs.AddAsync(log);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
