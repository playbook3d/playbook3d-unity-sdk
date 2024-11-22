using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PlaybookUnitySDK.Scripts
{
    public class PlaybookWebsocket : MonoBehaviour
    {
        private ClientWebSocket _webSocket;

        private const string PlaybookServerURL = "wss://echo.websocket.org";

        private async void Start()
        {
            await ConnectWebSocket(PlaybookServerURL);

            await SendMessageToWebSocket("Test");

            await ReceiveMessagesFromWebSocket();
        }

        private async void OnDestroy()
        {
            await CloseWebSocket();
        }

        private async Task ConnectWebSocket(string uri)
        {
            _webSocket = new ClientWebSocket();
            try
            {
                await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
                Debug.Log("WebSocket connected!");
            }
            catch (Exception e)
            {
                Debug.LogError($"WebSocket connection failed: {e.Message}");
            }
        }

        private async Task SendMessageToWebSocket(string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync
                (
                    new ArraySegment<byte>(messageBytes), 
                    WebSocketMessageType.Text, 
                    true,
                    CancellationToken.None
                );
            }
        }

        private async Task ReceiveMessagesFromWebSocket()
        {
            byte[] buffer = new byte[1024];
            while (_webSocket?.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync
                (
                    new ArraySegment<byte>(buffer), 
                    CancellationToken.None
                );

                if (result.MessageType != WebSocketMessageType.Close)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log($"Message received: {receivedMessage}");
                }
            }
        }

        private async Task CloseWebSocket()
        {
            if (_webSocket == null) return;            
            
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync
                (
                    WebSocketCloseStatus.NormalClosure, 
                    "Closing connection",
                    CancellationToken.None
                );
            }
            
            _webSocket.Dispose();
            _webSocket = null;
        }
    }
}
