﻿using System;

namespace TriangleStreamingServer.Models
{
	public class ChatAction
	{
		public string StreamId { get; set; }
		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
		public Type ActionType { get; set; }

		public enum Type
		{
			NONE = 0,
			MESSAGE = 1,
			JOIN = 2,
			LEAVE = 3
		}
	}
}