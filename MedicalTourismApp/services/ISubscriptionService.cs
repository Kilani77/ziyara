using MedicalTourismApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public interface ISubscriptionService
{
    Task<bool> NeedsSubscriptionAsync(string userId);
    Task<bool> HasActiveSubscriptionAsync(string userId);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(ApplicationDbContext context, ILogger<SubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> NeedsSubscriptionAsync(string userId)
    {
        try
        {
            // Simply reverse the HasActiveSubscription logic
            return !(await HasActiveSubscriptionAsync(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking subscription for user {userId}");
            return true; // Default to needing subscription if error occurs
        }
    }

    public async Task<bool> HasActiveSubscriptionAsync(string userId)
    {
        var provider = await _context.Serviceproviders
            .FirstOrDefaultAsync(sp => sp.UserId == userId);

        if (provider == null) return false;

        // Check if free month is still active
        bool hasFreeMonth = provider.IsFreeMonthActive &&
                            provider.MonthEndDate.HasValue &&
                            provider.MonthEndDate.Value >= DateTime.Now;
        bool hasSub = provider.IsSubscribe &&
                            provider.MonthEndDate.HasValue &&
                            provider.MonthEndDate.Value >= DateTime.Now;

        // User has access if either free month or paid subscription is active
        return hasFreeMonth || hasSub;
    }
}
