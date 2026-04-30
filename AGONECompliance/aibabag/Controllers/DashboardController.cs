using aibabag.Data;
using aibabag.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aibabag.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDetailedAstrologyService _detailedService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ApplicationDbContext context,
        IDetailedAstrologyService detailedService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _detailedService = detailedService;
        _logger = logger;
    }

    [HttpGet("full/{userId:int}")]
    public async Task<IActionResult> GetFullDashboard(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
            {
                return NotFound(new { message = "User not found." });
            }

            var detail = await _context.DetailedAstrologyInsights
                .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
            if (detail is null)
            {
                detail = await _detailedService.GenerateDetailedInsights(user, user.ZodiacSign, cancellationToken);
                _context.DetailedAstrologyInsights.Add(detail);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Ok(new
            {
                success = true,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.ZodiacSign,
                    user.ChineseZodiac
                },
                dashboard = detail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading full dashboard");
            return BadRequest(new { message = ex.Message });
        }
    }
}
