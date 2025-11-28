using BazeSec.Data;
using BazeSec.Models;
using BazeSec.DTOs.AnonymousTips;
using Microsoft.EntityFrameworkCore;

namespace BazeSec.Services
{
    public class AnonymousTipService
    {
        private readonly ApplicationDbContext _context;

        public AnonymousTipService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AnonymousTip> CreateTipAsync(CreateAnonymousTipDto dto, int reporterId, string reporterRole)
        {
            var tip = new AnonymousTip
            {
                Title = dto.Title,
                Category = dto.Category,
                Description = dto.Description,
                Location = dto.Location,
                PreferredContact = dto.PreferredContact,
                ReporterUserId = reporterId,
                ReporterRole = reporterRole
            };

            _context.AnonymousTips.Add(tip);
            await _context.SaveChangesAsync();

            return tip;
        }

        public async Task<List<AnonymousTip>> GetAllAsync()
        {
            return await _context.AnonymousTips
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<AnonymousTip> GetByIdAsync(int id)
        {
            return await _context.AnonymousTips.FindAsync(id);
        }

        public async Task<bool> UpdateStatusAsync(AnonymousTip t, string status, string priority, string notes, int handlerId, string handlerName)
        {
            t.Status = status;
            t.Priority = priority ?? t.Priority;
            t.InternalNotes = notes ?? t.InternalNotes;
            t.HandlerUserId = handlerId;
            t.HandlerName = handlerName;
            t.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
