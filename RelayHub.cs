using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace YOUVI.RelayServer
{
    public class RelayHub : Hub
    {
        private readonly Microsoft.Extensions.Logging.ILogger<RelayHub>? _logger;
        // clientId -> set of connectionIds
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> ClientConnections =
            new();

        // connectionId -> clientId
        private static readonly ConcurrentDictionary<string, string> ConnectionToClient =
            new();

        public RelayHub(Microsoft.Extensions.Logging.ILogger<RelayHub> logger)
        {
            _logger = logger;
        }

        public Task Register(string clientId)
        {
            var connId = Context.ConnectionId;
            var set = ClientConnections.GetOrAdd(clientId, _ => new ConcurrentDictionary<string, byte>());
            set[connId] = 0;
            ConnectionToClient[connId] = clientId;
            _logger?.LogInformation("Register: clientId {ClientId} connection {ConnId}", clientId, connId);
            return Task.CompletedTask;
        }

        public async Task SendTo(string targetClientId, string message)
        {
            if (ClientConnections.TryGetValue(targetClientId, out var set) && !set.IsEmpty)
            {
                var connectionIds = set.Keys.ToList();
                var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
                _logger?.LogInformation("SendTo: from {From} to {Target} via connections {Count}", from, targetClientId, connectionIds.Count);
                await Clients.Clients(connectionIds).SendAsync("ReceiveMessage", from, message);
            }
            else
            {
                _logger?.LogWarning("SendTo: target {Target} not found", targetClientId);
                await Clients.Caller.SendAsync("ClientNotFound", targetClientId);
            }
        }

        public async Task JoinCall(string callId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, callId);
            var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
            _logger?.LogInformation("JoinCall: connection {ConnId} (client {From}) joined group {CallId}", Context.ConnectionId, from, callId);
            await Clients.Group(callId).SendAsync("ParticipantJoined", from, callId);
        }

        public async Task LeaveCall(string callId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, callId);
            var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
            _logger?.LogInformation("LeaveCall: connection {ConnId} (client {From}) left group {CallId}", Context.ConnectionId, from, callId);
            await Clients.Group(callId).SendAsync("ParticipantLeft", from, callId);
        }

        public Task SendToGroup(string callId, string message)
        {
            var from = ConnectionToClient.TryGetValue(Context.ConnectionId, out var f) ? f : Context.ConnectionId;
            _logger?.LogInformation("SendToGroup: from {From} to group {CallId}", from, callId);
            return Clients.Group(callId).SendAsync("ReceiveMessage", from, message);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connId = Context.ConnectionId;
            if (ConnectionToClient.TryRemove(connId, out var clientId))
            {
                _logger?.LogInformation("OnDisconnected: connection {ConnId} removed for client {ClientId}", connId, clientId);
                if (ClientConnections.TryGetValue(clientId, out var set))
                {
                    set.TryRemove(connId, out _);
                    if (set.IsEmpty)
                    {
                        ClientConnections.TryRemove(clientId, out _);
                        _logger?.LogInformation("OnDisconnected: client {ClientId} has no more connections, removed", clientId);
                    }
                }
            }
            else
            {
                _logger?.LogInformation("OnDisconnected: connection {ConnId} had no mapped client", connId);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
