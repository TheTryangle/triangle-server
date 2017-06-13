using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triangle_Streaming_Server
{
	public class Stream
	{
		public string ClientID { get; private set; }
		public string Title { get; set; }
		public string StreamerName { get; set; }
		public Queue<byte[]> VideoQueue { get; private set; }
		public int FragmentCount { get; set; }

		public Stream(string clientID)
		{
			this.ClientID = clientID;
			this.VideoQueue = new Queue<byte[]>();
			this.FragmentCount = 0;
		}
	}
}
