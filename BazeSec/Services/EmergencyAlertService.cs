using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BazeSec.Data;
using BazeSec.DTOs.Emergencies;
using BazeSec.Models;
using Microsoft.EntityFrameworkCore;

namespace BazeSec.Services
{
    public class EmergencyAlertService
    {
        private readonly ApplicationDbContext _context;

        public EmergencyAlertService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EmergencyAlert> CreateAsync(CreateEmergencyAlertDto dto, int creatorId, string creatorName)
        {
            var alert = new EmergencyAlert
            {
                Title = dto.Title,
                Message = dto.Message,
                Severity = dto.Severity ?? "Info",
                Location = dto.Location,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.EmergencyAlerts.Add(alert);
            await _context.SaveChangesAsync();

            return alert;
        }

        public async Task<EmergencyAlert?> GetLatestActiveAsync()
        {
            return await _context.EmergencyAlerts
                .Where(a => a.Status == "Active")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<EmergencyAlert>> GetAllAsync()
        {
            return await _context.EmergencyAlerts
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<EmergencyAlert?> GetByIdAsync(int id)
        {
            return await _context.EmergencyAlerts.FindAsync(id);
        }

        public async Task<bool> ResolveAsync(EmergencyAlert alert, int adminId, string adminName, string? resolutionNote)
        {
            alert.Status = "Resolved";
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedByUserId = adminId;
            alert.ResolvedByName = adminName;
            alert.ResolutionNote = resolutionNote ?? alert.ResolutionNote;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var alert = await _context.EmergencyAlerts.FindAsync(id);
            if (alert == null) return false;

            _context.EmergencyAlerts.Remove(alert);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
