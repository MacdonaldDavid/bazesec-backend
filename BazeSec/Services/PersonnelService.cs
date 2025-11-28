using BazeSec.Models;
using BazeSec.DTOs;
using Microsoft.EntityFrameworkCore;
using BazeSec.Data;

namespace BazeSec.Services
{
    public class PersonnelService
    {
        private readonly ApplicationDbContext _context;

        public PersonnelService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // SEARCH + FILTER (SQL LEVEL) — Null-safe & Optimized
        // ============================================================
        public async Task<List<Personnel>> SearchAsync(string? q, string? status)
        {
            var query = _context.Personnel.AsQueryable();

            // Normalize
            if (!string.IsNullOrWhiteSpace(q)) q = q.Trim().ToLower();

            // Search filter (null-safe)
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(q)) ||
                    (p.Email != null && p.Email.ToLower().Contains(q)) ||
                    (p.Guardpost != null && p.Guardpost.ToLower().Contains(q))
                );
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToLower();
                query = query.Where(p =>
                    p.Status != null && p.Status.ToLower() == s
                );
            }

            return await query.ToListAsync();
        }

        // ============================================================
        // GET ALL (NO FILTERS)
        // ============================================================
        public async Task<List<Personnel>> GetAllAsync()
        {
            return await _context.Personnel.ToListAsync();
        }

        // ============================================================
        // GET BY ID
        // ============================================================
        public async Task<Personnel?> GetByIdAsync(int id)
        {
            return await _context.Personnel.FindAsync(id);
        }

        // ============================================================
        // CREATE
        // ============================================================
        public async Task<Personnel> CreateAsync(PersonnelCreateDTO dto)
        {
            var person = new Personnel
            {
                Name = dto.Name,
                Email = dto.Email,
                Status = dto.Status,
                Guardpost = dto.Guardpost
            };

            _context.Personnel.Add(person);
            await _context.SaveChangesAsync();

            return person;
        }

        // ============================================================
        // UPDATE
        // ============================================================
        public async Task<Personnel?> UpdateAsync(int id, PersonnelUpdateDTO dto)
        {
            var person = await _context.Personnel.FindAsync(id);
            if (person == null) return null;

            person.Name = dto.Name;
            person.Email = dto.Email;
            person.Status = dto.Status;
            person.Guardpost = dto.Guardpost;

            await _context.SaveChangesAsync();
            return person;
        }

        // ============================================================
        // DELETE
        // ============================================================
        public async Task<bool> DeleteAsync(int id)
        {
            var person = await _context.Personnel.FindAsync(id);
            if (person == null) return false;

            _context.Personnel.Remove(person);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
