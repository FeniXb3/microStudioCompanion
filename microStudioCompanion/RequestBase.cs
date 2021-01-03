using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Websocket.Client;

namespace microStudioCompanion
{
    public abstract class RequestBase
    {
        static int lastRequestId = 0;
        public int request_id { get; set; }
        public string name { get; set; }
        public bool Handled { get; set; }
        public static Dictionary<int, RequestBase> RequestsSent { get; set; } = new Dictionary<int, RequestBase>();
        public abstract string Message { get; }

        public RequestBase()
        {
            request_id = lastRequestId++;
        }

        public abstract string Serialize();

        public void SendVia(WebsocketClient socket)
        {
            lock (RequestsSent)
            {
                if (name != "ping")
                {
                    RequestsSent.Add(request_id, this);
                    Console.WriteLine(Message);
                }
                socket.Send(Serialize());
            }
        }

        internal static RequestType GetSentRequest<RequestType>(int requestId)
            where RequestType: RequestBase
        {
            return (RequestType)RequestsSent[requestId];
        }
    }
}
