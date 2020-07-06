using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace W3ChampionsStatisticService.Chats
{
    public class ChatHub : Hub
    {
        private readonly ChatAuthenticationService _authenticationService;
        private readonly ConnectionMapping _connections;
        private readonly ChatHistory _chatHistory;

        public ChatHub(
            ChatAuthenticationService authenticationService,
            ConnectionMapping connections,
            ChatHistory chatHistory)
        {
            _authenticationService = authenticationService;
            _connections = connections;
            _chatHistory = chatHistory;
        }

        public async Task SendMessage(string chatApiKey, string battleTag, string message)
        {
            var trimmedMessage = message.Trim();
            var user = await _authenticationService.GetUser(chatApiKey, battleTag);
            if (!string.IsNullOrEmpty(trimmedMessage))
            {
                var chatRoom = _connections.GetRoom(Context.ConnectionId);
                _chatHistory.AddMessage(chatRoom, user, trimmedMessage);
                await Clients.Group(chatRoom).SendAsync("ReceiveMessage", user, trimmedMessage);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = _connections.GetUser(Context.ConnectionId);
            if (user != null)
            {
                var chatRoom = _connections.GetRoom(Context.ConnectionId);
                _connections.Remove(Context.ConnectionId);
                await Clients.Group(chatRoom).SendAsync("UserLeft", user);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SwitchRoom(string chatApiKey, string battleTag, string chatRoom)
        {
            var user = await _authenticationService.GetUser(chatApiKey, battleTag);

            var oldRoom = _connections.GetRoom(Context.ConnectionId);
            _connections.Remove(Context.ConnectionId);
            _connections.Add(Context.ConnectionId, chatRoom, user);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldRoom);
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoom);

            var usersOfRoom = _connections.GetUsersOfRoom(chatRoom);
            await Clients.Group(oldRoom).SendAsync("UserLeft", user);
            await Clients.Group(chatRoom).SendAsync("UserEntered", user);
            await Clients.Caller.SendAsync("StartChat", usersOfRoom, _chatHistory.GetMessages(chatRoom));
        }

        public async Task LoginAs(string chatApiKey, string battleTag, string chatRoom)
        {
            var user = await _authenticationService.GetUser(chatApiKey, battleTag);

            if (!user.VerifiedBattletag)
            {
                await Clients.Caller.SendAsync("ChatKeyInvalid");
            }

            _connections.Add(Context.ConnectionId, chatRoom, user);
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoom);

            var usersOfRoom = _connections.GetUsersOfRoom(chatRoom);

            await Clients.Group(chatRoom).SendAsync("UserEntered", user);
            await Clients.Caller.SendAsync("StartChat", usersOfRoom, _chatHistory.GetMessages(chatRoom));
        }
    }
}