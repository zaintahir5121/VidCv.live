using CosmicMatch.Data;
using CosmicMatch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CosmicMatch.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
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
    public async Task<IActionResult> GetFullDashboard(int userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return NotFound(new { message = "User not found." });
            }

            var detail = await _context.DetailedAstrologyInsights
                .FirstOrDefaultAsync(d => d.UserId == userId);
            if (detail is null)
            {
                detail = _detailedService.GenerateDetailedInsights(user, user.ZodiacSign);
                _context.DetailedAstrologyInsights.Add(detail);
                await _context.SaveChangesAsync();
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
