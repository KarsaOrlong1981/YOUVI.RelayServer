// SignalR fallback sample for WebRTC webview
// Behavior: try local websocket first (preserves current local behavior). If not reachable, connect to Relay (SignalR).
nimport * as signalR from "@microsoft/signalr";
nconst hubUrl = window.relayUrl || "https://your-relay.example.com/webrtchub";
const clientId = window.clientId || "mobile-client-456";
const localIp = window.localIPAddress;
const wsPort = window.websocketPort;
let signalRConnection;
nfunction tryLocalWebSocketConnect(ip, port, timeoutMs = 1500) {
  return new Promise((resolve, reject) => {
    try {
      const url = `ws://${ip}:${port}/webrtcSip`;
      const ws = new WebSocket(url);
      let settled = false;
      const timer = setTimeout(() => {
        if (!settled) {
          settled = true;
          try { ws.close(); } catch (e) {}
          reject(new Error('local-ws-timeout'));
        }
      }, timeoutMs);
n      ws.onopen = () => {
        if (!settled) {
          settled = true;
          clearTimeout(timer);
          resolve(ws);
        }
      };
      ws.onerror = (e) => {
        if (!settled) {
          settled = true;
          clearTimeout(timer);
          reject(new Error('local-ws-error'));
        }
      };
    } catch (ex) {
      reject(ex);
    }
  });
}
nasync function startSignalR() {
  signalRConnection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect()
    .build();
n  signalRConnection.on("ReceiveMessage", (from, msg) => {
    const data = typeof msg === 'string' ? JSON.parse(msg) : msg;
    // existing handler for messages from the bridge
    handleSignalingMessage(data);
  });
n  signalRConnection.on("ParticipantJoined", (who, callId) => {
    console.log('Participant joined', who, callId);
  });
n  await signalRConnection.start();
  await signalRConnection.invoke('Register', clientId);
}
n// called by native code after setting window.localIPAddress / window.websocketPort / window.clientId
export async function initializeWebSocketFromDotNet() {
  // try local websocket first (no change to existing behavior)
  if (localIp && wsPort) {
    try {
      const ws = await tryLocalWebSocketConnect(localIp, wsPort, 1200);
      // attach existing websocket handlers to ws (implementation dependent)
      setupLocalWebSocket(ws); // assume this exists in the webview assets
      return;
    } catch (e) {
      console.warn('local websocket not reachable, falling back to relay', e);
    }
  }
n  // fallback: connect to relay via SignalR (on-demand when a call arrives)
  await startSignalR();
  // if a callId was provided by the server, join the call group so you receive group messages
  if (window.callId) {
    try {
      await signalRConnection.invoke('JoinCall', window.callId);
    } catch (ex) {
      console.warn('JoinCall failed', ex);
    }
  }
}
nexport async function sendToCall(callId, payload) {
  if (!signalRConnection) await startSignalR();
  await signalRConnection.invoke('SendToGroup', callId, JSON.stringify(payload));
}
