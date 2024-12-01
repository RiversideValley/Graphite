using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using System.Security.Claims;

namespace FireCore.Services;


    public static class UserHandler
{
    public static HashSet<KeyValuePair<string, string>> ConnectedIds = new HashSet<KeyValuePair<string, string>>();
}

public class HubCore : Hub
{
    private readonly GraphServiceClient _graphServiceClient;
    private User? _currentUser;

    public HubCore(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
        Console.WriteLine("Initializing Hub Core Connection...");
    }

    public override async Task OnConnectedAsync()
    {
        var userIdentifier = Context.UserIdentifier;
        if (userIdentifier != null)
        {
            lock (UserHandler.ConnectedIds)
            {
                UserHandler.ConnectedIds.Add(new KeyValuePair<string, string>(Context.ConnectionId, userIdentifier));
            }

            await Clients.All.SendAsync("ReceiveBroadcast", Context.User, "Welcome to Fire Chat.", 0);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name;
        if (userName != null)
        {
            lock (UserHandler.ConnectedIds)
            {
                UserHandler.ConnectedIds.Remove(new KeyValuePair<string, string>(Context.ConnectionId, userName));
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    [HubMethodName("SigUserUpdate")]
    public async Task SigUserUpdate()
    {
        var uniqueUsers = UserHandler.ConnectedIds.Select(x => x.Value).Distinct().ToList();
        await Clients.All.SendAsync("SigUserUpdate", JsonConvert.SerializeObject(uniqueUsers));
    }

    [HubMethodName("SendMessage")]
    public async Task SendMessage(string user, string message, string priority)
    {
        await Clients.All.SendAsync("ReceiveBroadcast", user, message, priority);
    }

    [HubMethodName("SendOneMessage")]
    public async Task SendOneMessage(string idUser, string user, string message, string priority)
    {
        var recipientConnections = UserHandler.ConnectedIds
            .Where(x => x.Value == idUser)
            .Select(x => x.Key)
            .ToList();

        if (recipientConnections.Any())
        {
            foreach (var connectionId in recipientConnections)
            {
                await Clients.Client(connectionId).SendAsync("ReceiveBroadcast", user, message, priority);
            }
        }
        else
        {
            await Clients.All.SendAsync("ReceiveBroadcast", user, message, priority);
        }
    }

    [HubMethodName("Greetings")]
    public async Task Greetings(string user, string message, string priority)
    {
        user = "Administrators";
        await Clients.All.SendAsync("ReceiveBroadcast", user, message, priority);
    }
}
