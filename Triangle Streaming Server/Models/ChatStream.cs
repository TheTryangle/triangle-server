using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using TriangleStreamingServer.Extensions;
using TriangleStreamingServer.WebSockets;

namespace TriangleStreamingServer.Models
{
    public class ChatStream : WebSockets.WebSocketHandler
    {
        private static string _name;

        

        public ChatStream(WebSocketConnectionManager webSocketConnectionManager, StreamQueueManager streamManager) : base(webSocketConnectionManager, streamManager)
        {
            Clients = new Dictionary<string, string>();
        }

        public static Dictionary<string, string> Clients { get; private set; }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);

            var socketId = WebSocketConnectionManager.GetId(socket);
            Console.WriteLine("Someone Connected");
        }

        public override Task OnDisconnected(WebSocket socket)
        {
            string id = WebSocketConnectionManager.GetId(socket);

            Clients.Remove(id);
            return base.OnDisconnected(socket);
            Console.WriteLine("Someone disconnected");
            //Sessions.Broadcast(String.Format("{0} logged off...", _name));

        }

        public override async Task OnMessage(WebSocket socket, WebSocketReceiveResult result, WebSocketMessageType type, byte[] buffer)
        {
            if (type != WebSocketMessageType.Text)
            {
                throw new NotImplementedException();
            }
            string data = Encoding.UTF8.GetString(buffer).Trim('\0');
            string id = WebSocketConnectionManager.GetId(socket);
            Console.WriteLine(data);

            if (data.StartsWith("JOIN"))
            {
                //Get data from json
                var json = data.Replace("JOIN ", "");
                var jsonData = (JObject)JsonConvert.DeserializeObject(json);
                string streamid = jsonData["StreamID"].ToString();

                if (Clients.ContainsKey(id))
                {
                    Clients.Remove(id);
                }

                Clients.Add(id, streamid);

                var _list = Clients.Where(p => p.Value == streamid).Select(p => p.Key).ToArray();
                //await SendToAll(json, WebSocketConnectionManager.GetAll().Keys.ToArray());
                await SendToAll(json, _list);
            }
            else if (data.StartsWith("NAME"))
            {
                var json = data.Replace("NAME ", "");
                _name = json;
                await SendToAll(String.Format("{0} logged in...", _name, WebSocketConnectionManager.GetAll().Keys.ToArray()));

            }
            
        }
    }
}