using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace microStudioCompanion.Extensions
{
    static class WebSocketExtensions
    {
        public static ResponseType SendAndReceive<RequestType, ResponseType>(this ClientWebSocket socket, RequestType requestData)
            where RequestType : RequestBase
            where ResponseType : ResponseBase
        {
            var requestText = JsonSerializer.Serialize(requestData);

            //Console.WriteLine($"Sending {requestData.name} request");
            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestText)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            var buffer = new byte[99999999];
            //Console.WriteLine($"Receiving {requestData.name} response");

            var result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
            var resultText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = JsonSerializer.Deserialize<ResponseType>(resultText);

            return response;
        }

        public static void SendRequest<RequestType>(this ClientWebSocket socket, RequestType requestData)
            where RequestType : RequestBase
        {
            var requestText = JsonSerializer.Serialize(requestData);

            //Console.WriteLine($"Sending {requestData.name} request");
            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestText)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }
    }
}
