# YOUVI Relay Server (PoC)

Kurzanleitung (PoC, .NET 10):

1. Build: `dotnet build`
2. Run (lokal): `dotnet run --urls http://localhost:5002`

Endpunkte:
- GET / -> "YOUVI Relay Server"
- SignalR Hub: `/webrtchub`

SignalR API (PoC):
- `Register(string clientId)` - registriert eine clientId -> connectionId Zuordnung
- `SendTo(string targetClientId, string message)` - sendet `message` an `targetClientId`
- Server sendet `ReceiveMessage(fromConnectionId, message)` an den Ziel-Client
- Wenn Ziel nicht verbunden ist, wird `ClientNotFound` an den Sender gesendet

Wichtig:
- Für Tests ist CORS `AllowAll` aktiviert. In Produktion unbedingt Origins einschränken und Auth hinzufügen.
- Signalisierung nur; Medien (RTP/WebRTC) sind weiterhin P2P (oder über TURN wenn nötig).

Samples: `samples/csharp-client.cs`, `samples/webrtc-signalr.js`

Deployment (kurz):
- Railway, Fly.io oder Render unterstützen .NET Apps. Für PoC genügt Railway (GitHub-Repo oder Dockerfile).

