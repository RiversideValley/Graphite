using System.Data;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using FireCore.Services.Contracts.MessageHandler;
using FireCore.Services.Contracts.SessionHandler.AzureTableSessionStorage;





namespace FireCore.Services.Hubs
{


    public class AzureChat : Hub
    {
        private readonly IMessageHandler _messageHandler;
        private readonly SignalRService _signalRService;
        private readonly IHubCommander _commander;
        private readonly HttpClient? _httpClient;
        private readonly IAzureDataTableSessionStorage _azureDataTableSessionStorage;

        public static HashSet<KeyValuePair<string, string>> ConnectedIds { get; set;  } 


        public AzureChat(IMessageHandler messageHandler, SignalRService rService, IHubCommander hubCommander,  HttpClient httpClient, IAzureDataTableSessionStorage azureDataTable)
        {
            _messageHandler = messageHandler;
			ConnectedIds = new HashSet<KeyValuePair<string, string>>();	
            //_sessionHandler = sessionHandler;
            _signalRService = rService;
            _commander = (IHubCommander)hubCommander;
            //_graphServiceClient = graphServiceClient;
            _httpClient = httpClient;
            _azureDataTableSessionStorage = azureDataTable;
        }

        public override async Task OnConnectedAsync()
        {
            var sender = Context.UserIdentifier;
            var lockit = new object();
            lock (lockit)
            {
                if (Context.UserIdentifier != null)
                    ConnectedIds.Add(new KeyValuePair<string, string>(Context.ConnectionId, Context.UserIdentifier));
            }
            await Clients!.All.SendAsync("showLoginUsers", ConnectedIds.Select(x => x.Value).ToArray());


            // Gather user session from database
            //var userSessions = await _sessionHandler.GetLatestSessionsAsync(sender!);
            var userSessions = await _azureDataTableSessionStorage.GetLatestSessionsAsync(sender!);

            //  Push the latest session information to the commander.
            await AddSessionsToCommander(userSessions!);

            //  Send to latest session list to user.
            await Clients!.Caller.SendAsync("updateSessions", userSessions);

            var onConnectedMessage = sender + " joined the chat room";
            var message = new Message("Public", DateTime.Now, onConnectedMessage, "Sent");

            await Clients!.All.SendAsync("sendNotify", onConnectedMessage);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var sender = Context.UserIdentifier;
            var lockit = new object();
            lock (lockit)
            {
                if (Context.UserIdentifier != null)
                    ConnectedIds.Remove(new KeyValuePair<string, string>(Context.ConnectionId, Context.UserIdentifier));
            }
            await Clients!.All.SendAsync("showLoginUsers", ConnectedIds.Select(x => x.Value).ToArray());

            var listofActive = _commander.ActiveGroups;

            foreach (var group in listofActive)
            {
                if (group.Key.Item1 == Context.ConnectionId)
                {
                    await _commander.DeleteAsync(new Tuple<string, string>(group.Key.Item1, group.Key.Item2));
                }
            }

            var onDisconnectedMessage = sender + " left the chat room";
            var message = new Message("Public", DateTime.Now, onDisconnectedMessage, "Sent");
            await Clients!.All.SendAsync("sendNotify", onDisconnectedMessage);
            await base.OnDisconnectedAsync(exception);

        }

        public async Task AddSessionsToCommander(KeyValuePair<string, Contracts.SessionHandler.Session>[] userSessions)
        {

            await _commander.DeleteAllSessionsByUser(Context.ConnectionId);

            foreach (var session in userSessions)
            {
                await _commander.WriteAsync(new Data.Models.SessionDbEntity(Context.ConnectionId, session.Key, session.Value.SessionId, DateTimeOffset.Now));
                await AddToGroup(session.Key);
            }

        }

        public async Task AddToGroup(string groupName)
        {
            await Groups!.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task RemoveFromGroup(string groupName)
        {
            await Groups!.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }


        public async Task<string> BroadcastMessage(string messageContent)
        {
            var sender = Context.UserIdentifier;
            var message = new Message(sender, DateTime.Now, messageContent, "Sent");
            var sequenceId = await _messageHandler.AddNewMessageAsync("Public", message);

            await Clients!.Others.SendAsync("displayUserMessage", "Public", sequenceId, sender, messageContent);

            return sequenceId;
        }

        public async Task<string> GetOrCreateSession(string receiver)
        {
            var sender = Context.UserIdentifier;
            //var session = await _sessionHandler.GetOrCreateSessionAsync(sender!, receiver);
            //var isSession = await _sessionHandler.GetSessionBySessionId(session.SessionId);
            var session = await _azureDataTableSessionStorage.GetOrCreateSessionAsync(sender!, receiver);
            var isSession = await _azureDataTableSessionStorage.GetSessionBySessionId(session.SessionId);

            var isListedCommander = _commander.ActiveGroups.ToDictionary().Where(w => w.Key.Item2 == receiver).Where(x => x.Value == session.SessionId).ToDictionary();

            if (await _signalRService.MessageHubContext!.ClientManager.UserExistsAsync(receiver))
            {
                //var userSessions = await _sessionHandler.GetLatestSessionsAsync(receiver);
                var userSessions = await _azureDataTableSessionStorage.GetLatestSessionsAsync(sender!);
                await Clients!.User(receiver).SendAsync("updateSessions", userSessions!);
                await AddSessionsToCommander(userSessions);
            }
            else
            {

                //var userSessions = await _sessionHandler.GetLatestSessionsAsync(sender!);
                var userSessions = await _azureDataTableSessionStorage.GetLatestSessionsAsync(sender!);
                //await Clients!.User(sender!).SendAsync("updateSessions", userSessions!);
                await AddSessionsToCommander(userSessions);
            }

            await Task.Delay(400);
            return session.SessionId;

        }
        public async Task<string> SendUserMessage(string sessionId, string roomName, string messageContent)
        {
            roomName = roomName.ToLower();
            var sender = Context.UserIdentifier;
            var message = new Message(sender!, DateTime.Now, messageContent, "Sent");
            var sequenceId = await _messageHandler.AddNewMessageAsync(sessionId, message);

            // see if there are any sessions
            //var listOfSessions = await _sessionHandler.GetSessionBySessionId(sessionId) ?? null;

            var listOfSessions = await _azureDataTableSessionStorage.GetSessionBySessionId(sessionId) ?? null;

            if (listOfSessions is null)
            {
                await _commander.WriteAsync(new Data.Models.SessionDbEntity(Context.ConnectionId, roomName, sessionId, DateTimeOffset.Now));
                await AddToGroup(roomName);
                await Clients!.GroupExcept(roomName, Context.ConnectionId).SendAsync("displayUserMessage", sessionId, sequenceId, roomName, messageContent);
            }

            //  get all user that are federated
            var hubUsers = _commander.MsalCurrentUsers!.ToList();

            // this means that the Room is actually a private chat - "An Auth User", so roll 
            var twins = (from v in hubUsers where v == roomName select v).Distinct();

            // 1. send update to group or private chat.  2. send owner update

            if (twins.Count() > 0)
            {    // private chat
                await Clients!.User(roomName).SendAsync("displayUserMessage", sessionId, sequenceId, Context.UserIdentifier!, messageContent);
                await Clients!.Caller.SendAsync("displayResponseMessage", sessionId, sequenceId, "Sent");
                await Clients!.User(roomName).SendAsync("sendPrivateMessage", sessionId, string.Format("You have a private message from: {0}", Context!.UserIdentifier), messageContent, sender!);

            }
            else
            {
                // group message 
                await Clients!.GroupExcept(roomName, Context.ConnectionId).SendAsync("displayUserMessage", sessionId, sequenceId, sender!, messageContent);
                //await Clients!.Caller.SendAsync("displayResponseMessage", sessionId, sequenceId, "Sent");
            }

            return sessionId;


        }

        public async Task<string> SendUserResponse(string sessionId, string sequenceId, string receiver, string messageStatus)
        {
            await _messageHandler.UpdateMessageAsync(sessionId, sequenceId, messageStatus);

            await Clients!.User(receiver).SendAsync("displayResponseMessage", sessionId, sequenceId, messageStatus);

            return messageStatus;
        }


        public async Task<Task> DeleteUserSession(string userName, string partnerName)
        {
            var sender = Context.UserIdentifier;
            //var items = await _sessionHandler.GetLatestSessionsAsync(userName);
            var items = await _azureDataTableSessionStorage.GetLatestSessionsAsync(userName);
            var Removed = (from kv in items where kv.Key == partnerName select kv);
            // delete session from table 
            //await _sessionHandler.DeleteUserSession(userName, partnerName);
            await _azureDataTableSessionStorage.DeleteUserSession(userName, partnerName);

            await Task.Delay(400);
            // delete from static controller
            await _commander.DeleteAsync(new Tuple<string, string>(Context.ConnectionId, partnerName));
            await Task.Delay(400);
            //ActiveGroups.Remove(new Tuple<string, string>(Context.ConnectionId, partnerName));
            // remove from siganlr groups 
            await RemoveFromGroup(partnerName);
            //  get latest session information to the user.
            //var userSessions = await _sessionHandler.GetLatestSessionsAsync(userName);
            var userSessions = await _azureDataTableSessionStorage.GetLatestSessionsAsync(userName);
            //  Send to latest session list to user.
            //await Clients!.User(userName).SendAsync("deletedRoom", Removed.FirstOrDefault());
            await Task.Delay(400);
            if (userSessions.Count() > 0)
                await Clients!.User(userName).SendAsync("updateSessions", userSessions);


            return Task.CompletedTask;
        }
        public async Task<List<Message>> LoadMessages(string sessionId)
        {
            return await _messageHandler.LoadHistoryMessageAsync(sessionId);
        }
    }
}
