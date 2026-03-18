// C# SignalR client sample (groups / calls)
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
nclass Program {
    static async Task Main() {
        var hubUrl = "http://your-relay.example.com/webrtchub";
        var clientId = "doorstation-1";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();
n        connection.On<string, string>("ReceiveMessage", (from, msg) => {
            Console.WriteLine($"Received from {from}: {msg}");
            // Hier Signalisierungsnachricht verarbeiten und an SIP->WebRTC-Bridge weiterleiten
        });
n        connection.On<string, string>("ParticipantJoined", (who, callId) => {
            Console.WriteLine($"Participant {who} joined call {callId}");
        });
n        await connection.StartAsync();
        await connection.InvokeAsync("Register", clientId);
n        // Start a call -> create a callId and join group
        var callId = Guid.NewGuid().ToString();
        await connection.InvokeAsync("JoinCall", callId);
n        // Example: send offer to everyone in the call group
        var offerJson = "{\"type\":\"offer\",\"sdp\":\"...\"}";
        await connection.InvokeAsync("SendToGroup", callId, offerJson);
n        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
        await connection.StopAsync();
    }
}
