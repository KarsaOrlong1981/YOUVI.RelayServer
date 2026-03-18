using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace YOUVI.RelayServer
{
    public class RelayHub : Hub
    {
        // clientId -> set of connectionIds
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> ClientConnections =
            new();

        // connectionId -> clientId
        private static readonly ConcurrentDictionary<string, string> ConnectionToClient =
            new();

        public Task Register(string clientId)
        {
            var connId = Context.ConnectionId;
            var set = ClientConnections.GetOrAdd(clientId, _ => new ConcurrentDictionary<string, byte>());
            set[connId] = 0;
            ConnectionToClient[connId] = clientId;
            return Task.CompletedTask;
        }

        public async Task SendTo(string targetClientId, string message)
        {
            if (ClientConnections.TryGetValue(targetClientId, out var set) && !set.IsEmpty)
            {
                var connectionIds = set.Keys.ToList();
                var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
                await Clients.Clients(connectionIds).SendAsync("ReceiveMessage", from, message);
            }
            else
            {
                await Clients.Caller.SendAsync("ClientNotFound", targetClientId);
            }
        }

        public async Task JoinCall(string callId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, callId);
            var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
            await Clients.Group(callId).SendAsync("ParticipantJoined", from, callId);
        }

        public async Task LeaveCall(string callId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, callId);
            var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
            await Clients.Group(callId).SendAsync("ParticipantLeft", from, callId);
        }

        public Task SendToGroup(string callId, string message)
        {
            var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
            return Clients.Group(callId).SendAsync("ReceiveMessage", from, message);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connId = Context.ConnectionId;
            if (ConnectionToClient.TryRemove(connId, out var clientId))
            {
                if (ClientConnections.TryGetValue(clientId, out var set))
                {
                    set.TryRemove(connId, out _);
                    if (set.IsEmpty)
                        ClientConnections.TryRemove(clientId, out _);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
