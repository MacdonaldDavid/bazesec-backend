using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BazeSec.Data;
using BazeSec.DTOs;
using BazeSec.Models;
using Microsoft.EntityFrameworkCore;

namespace BazeSec.Services
{
    public class KeyService
    {
        private readonly ApplicationDbContext _context;

        private static readonly List<string> AllowedLocations = new()
        {
            "FrontGate",
            "Block A",
            "Block B",
            "Block C",
            "Block D",
            "Block E",
            "Block F"
        };

        public KeyService(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsValidLocation(string loc) =>
            AllowedLocations.Contains(loc, StringComparer.OrdinalIgnoreCase);

        public async Task<Dictionary<string, List<KeyItem>>> GetAllGroupedAsync()
        {
            var keys = await _context.KeyItems.ToListAsync();

            return AllowedLocations.ToDictionary(
                loc => loc,
                loc => keys.Where(k => k.Location == loc).ToList()
            );
        }

        public async Task<List<KeyItem>> GetByLocationAsync(string location)
        {
            if (!IsValidLocation(location)) return new();
            return await _context.KeyItems.Where(k => k.Location == location).ToListAsync();
        }

        public async Task<KeyItem?> GetByIdAsync(int id)
        {
            return await _context.KeyItems.FindAsync(id);
        }

        public async Task<KeyItem?> CreateAsync(KeyCreateDTO dto)
        {
            if (!IsValidLocation(dto.Location))
                return null;

            var key = new KeyItem
            {
                Name = dto.Name,
                Location = dto.Location,
                Status = "Available"
            };

            _context.KeyItems.Add(key);
            await _context.SaveChangesAsync();
            return key;
        }

        public async Task<KeyItem?> UpdateAsync(int id, KeyUpdateDTO dto)
        {
            var key = await _context.KeyItems.FindAsync(id);
            if (key == null) return null;
            if (!IsValidLocation(dto.Location)) return null;

            key.Name = dto.Name;
            key.Location = dto.Location;
            key.Status = dto.Status;

            await _context.SaveChangesAsync();
            return key;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var key = await _context.KeyItems.FindAsync(id);
            if (key == null) return false;

            _context.KeyItems.Remove(key);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<KeyItem?> CheckoutAsync(
            int keyId,
            string scannedLocation,
            int borrowerId,
            string borrowerName,
            string borrowerRole)
        {
            var key = await _context.KeyItems.FindAsync(keyId);
            if (key == null) return null;

            if (!IsValidLocation(scannedLocation) || key.Location != scannedLocation)
                throw new InvalidOperationException("QR code does not match key location.");

            if (key.Status == "CheckedOut")
                throw new InvalidOperationException("Key is already checked out.");

            key.Status = "CheckedOut";
            key.BorrowerId = borrowerId;
            key.BorrowerName = borrowerName;
            key.BorrowerRole = borrowerRole;
            key.CheckedOutAt = DateTime.UtcNow;
            key.CheckedInAt = null;

            _context.KeyLogs.Add(new KeyLog
            {
                KeyItemId = key.Id,
                BorrowerId = borrowerId,
                BorrowerName = borrowerName,
                BorrowerRole = borrowerRole,
                Location = key.Location,
                Action = "CheckOut",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return key;
        }

        public async Task<KeyItem?> CheckinAsync(
            int keyId,
            string scannedLocation,
            int borrowerId,
            string borrowerName,
            string borrowerRole)
        {
            var key = await _context.KeyItems.FindAsync(keyId);
            if (key == null) return null;

            if (!IsValidLocation(scannedLocation) || key.Location != scannedLocation)
                throw new InvalidOperationException("QR code does not match key location.");

            if (key.Status != "CheckedOut")
                throw new InvalidOperationException("Key is not currently checked out.");

            key.Status = "Available";
            key.CheckedInAt = DateTime.UtcNow;

            _context.KeyLogs.Add(new KeyLog
            {
                KeyItemId = key.Id,
                BorrowerId = borrowerId,
                BorrowerName = borrowerName,
                BorrowerRole = borrowerRole,
                Location = key.Location,
                Action = "CheckIn",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return key;
        }

        public async Task<List<KeyLog>> GetLogsForKeyAsync(int keyId)
        {
            return await _context.KeyLogs
                .Where(l => l.KeyItemId == keyId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
