using System;

namespace TriangleStreamingServer.Models
{
	public class ChatMessage
	{
		public string StreamId { get; set; }
		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
	}
}