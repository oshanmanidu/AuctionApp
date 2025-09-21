using AuctionApi.Data;
using AuctionApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class AuctionBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer? _timer;

    public AuctionBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Check every 60 seconds
        _timer = new Timer(CheckForExpiredAuctions, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private async void CheckForExpiredAuctions(object? state)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        var now = DateTime.UtcNow;

        // Find all open auctions where BidEndTime has passed
        var expiredItems = await context.AuctionItems
            .Include(a => a.Bids)
                .ThenInclude(b => b.User)
            .Where(a => a.BidEndTime < now && !a.IsClosed)
            .ToListAsync();

        foreach (var item in expiredItems)
        {
            var highestBid = item.Bids
                .OrderByDescending(b => b.Amount)
                .FirstOrDefault();

            if (highestBid != null)
            {
                var winner = highestBid.User;

                // 📧 Send Winner Email
                try
                {
                    await emailService.SendEmailAsync(
                        winner.Email,
                        $"🎉 Congratulations! You Won '{item.Name}'",
                        $@"
                        <h2>Congratulations, {winner.Email}!</h2>
                        <p>You've won the auction for <strong>{item.Name}</strong> with a bid of <strong>${highestBid.Amount:F2}</strong>.</p>
                        <p>Contact the admin to complete the payment.</p>
                        <p><em>Thank you for using BiddingBoom!</em></p>"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send winner email: {ex.Message}");
                }

                // 📧 Notify Other Bidders (Losers)
                var otherBidders = item.Bids
                    .Where(b => b.UserId != winner.Id)
                    .Select(b => b.User)
                    .Distinct();

                foreach (var user in otherBidders)
                {
                    try
                    {
                        await emailService.SendEmailAsync(
                            user.Email,
                            $"🔚 Auction for '{item.Name}' Has Ended",
                            $@"
                            <h2>Hello,</h2>
                            <p>The auction for <strong>{item.Name}</strong> has ended.</p>
                            <p>The winning bid was <strong>${highestBid.Amount:F2}</strong>.</p>
                            <p>Better luck next time!</p>
                            <p><em>Thanks for participating in BiddingBoom!</em></p>"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send email to {user.Email}: {ex.Message}");
                    }
                }

                // 🔒 Mark as closed to prevent re-processing
                item.IsClosed = true;
            }
            else
            {
                // No bids were placed
                try
                {
                    await emailService.SendEmailAsync(
                        item.User.Email,
                        $"📢 No Bids Placed for '{item.Name}'",
                        $"<p>No bids were placed on your auction '<strong>{item.Name}</strong>'. The auction has ended.</p>"
                    );
                }
                catch { /* Log if needed */ }
            }
        }

        // Save changes
        await context.SaveChangesAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}