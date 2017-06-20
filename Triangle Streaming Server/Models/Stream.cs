using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TriangleStreamingServer.Models
{
	public class Stream
	{
		public string ClientID { get; private set; }
		public string Title { get; set; }
		public string StreamerName { get; set; }

		//This info is used internally, and should not be sent to clients.
		[JsonIgnore]
		public Queue<byte[]> VideoQueue { get; private set; }

		[JsonIgnore]
		public AsymmetricKeyParameter PublicKey { get; set; }

		[JsonIgnore]
		public byte[] LatestSignature { get; set; }

		public Stream(string clientID)
		{
			this.ClientID = clientID;
			this.VideoQueue = new Queue<byte[]>();
		}
	}
}
