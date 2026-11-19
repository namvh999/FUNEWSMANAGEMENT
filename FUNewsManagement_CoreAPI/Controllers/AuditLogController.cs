using FUNewsManagement_CoreAPI.Data;
using FUNewsManagement_CoreAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagement_CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly FUNewsDbContext _context;

        public AuditLogController(FUNewsDbContext context)
        {
            _context = context;
        }

        // GET: /api/auditlog
        // Filter by user, entity, action
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? user,
            [FromQuery] string? entity,
            [FromQuery] string? action,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(user))
            {
                var u = user.Trim().ToLower();
                query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(u)) ||
                                         (a.UserId != null && a.UserId.ToLower().Contains(u)));
            }

            if (!string.IsNullOrWhiteSpace(entity))
            {
                var e = entity.Trim().ToLower();
                query = query.Where(a => a.Entity.ToLower().Contains(e));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                var act = action.Trim().ToUpper();
                query = query.Where(a => a.Action.ToUpper() == act);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.Timestamp <= endOfDay);
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    AuditID = a.AuditID,
                    UserId = a.UserId,
                    UserName = a.UserName,
                    Action = a.Action,
                    Entity = a.Entity,
                    EntityId = a.EntityId,
                    BeforeData = a.BeforeData,
                    AfterData = a.AfterData,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                data = items
            });
        }
    }
}
