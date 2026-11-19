using Microsoft.AspNetCore.SignalR;

namespace FUNewsManagement_CoreAPI.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string title, string message, string author)
        {
            await Clients.All.SendAsync("ReceiveNotification", new
            {
                title,
                message,
                author,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
