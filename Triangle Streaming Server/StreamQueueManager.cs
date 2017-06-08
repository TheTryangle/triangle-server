using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Triangle_Streaming_Server
{
	public class StreamQueueManager
	{
		private static StreamQueueManager _instance;

		public static StreamQueueManager GetInstance()
		{
			if (_instance == null)
				_instance = new StreamQueueManager();

			return _instance;
		}

		public Queue<byte[]> StreamQueue { get; set; }

		private StreamQueueManager()
		{
			StreamQueue = new Queue<byte[]>();
			RunCheckQueue();
		}

		public void AddToQueue(byte[] item)
		{
			StreamQueue.Enqueue(item);
		}

		public void RunCheckQueue()
		{
			Timer t = new Timer(250);
			t.Elapsed += T_Elapsed;
			t.Start();
		}

		private void T_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (StreamQueue.Count > 0)
			{
				byte[] nextVideo = StreamQueue.Dequeue();
				WebSocketManager.GetInstance().Server.WebSocketServices["/receive"].Sessions.Broadcast(nextVideo);
			}
		}
	}
}
