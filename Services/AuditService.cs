// Services/AuditService.cs
using System.Text.Json;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, int? entityId, object? data, string? reason, string userId);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;
        public AuditService(ApplicationDbContext db) => _db = db;

        public async Task LogAsync(string action, string entityType, int? entityId, object? data, string? reason, string userId)
        {
            var entry = new AuditEntry
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                DataJson = data == null ? null : JsonSerializer.Serialize(data),
                Reason = reason,
                PerformedByUserId = userId
            };
            _db.AuditEntries.Add(entry);
            await _db.SaveChangesAsync();
        }
    }
}
