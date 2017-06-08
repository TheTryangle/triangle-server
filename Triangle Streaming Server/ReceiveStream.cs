using System;
using WebSocketSharp.Server;

namespace Triangle_Streaming_Server
{
	public class ReceiveStream : WebSocketBehavior
	{
		protected override void OnOpen()
		{
			Console.WriteLine($"Someone connected to receive, {this.Context.UserEndPoint.ToString()}");
		}
	}
}
