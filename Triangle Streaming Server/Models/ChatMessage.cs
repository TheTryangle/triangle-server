using Newtonsoft.Json;
using System;
using TriangleStreamingServer.Converters;

namespace TriangleStreamingServer.Models
{
	public class ChatAction
	{
		public string StreamId { get; set; }

		[JsonConverter(typeof(DateTimeEpochConverter))]
		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
		public Type ActionType { get; set; }
		public string Name { get; set; }

		public enum Type
		{
			NONE = 0,
			MESSAGE = 1,
			JOIN = 2,
			LEAVE = 3
		}
	}
}