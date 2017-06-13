using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Triangle_Streaming_Server
{
	public class ReceiveStream : WebSocketBehavior
	{
		private static AsymmetricCipherKeyPair _keyPair;
		private static SHA1CryptoServiceProvider _sha1;

		private static string _pubKey;

		//A dictionary which keeps track of which clients are watching which streams.
		//Key is client session ID, value is stream ID.
		//TODO: optimize. Searching on value is O(n).
		public static Dictionary<string, string> Clients { get; private set; }

		public static void Initialize()
		{
			_sha1 = new SHA1CryptoServiceProvider();

			//Read privatekey.pem from executable directory
			using (var reader = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + @"\privatekey.pem"))
			{
				_keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();
			}

			//Get public key
			TextWriter textWriter = new StringWriter();
			PemWriter pemWriter = new PemWriter(textWriter);
			pemWriter.WriteObject(_keyPair.Public);
			pemWriter.Writer.Flush();
			_pubKey = textWriter.ToString();
			textWriter.Close();

			Clients = new Dictionary<string, string>();
		}

		protected override void OnOpen()
		{
			Console.WriteLine($"Someone connected to receive, {this.Context.UserEndPoint.ToString()}");
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			if(e.Data.Equals("PUBKEY"))
			{
				//Send public key to client
				Send(_pubKey);
			}
			else if(e.Data.StartsWith("WATCH "))
			{
				//Get the ID specified in the WATCH command (for example, "WATCH 158sadsa489g")
				string streamToWatch = e.Data.Remove(0, 6);

				if (Clients.ContainsKey(this.ID))
				{
					Clients.Remove(this.ID);
				}

				Clients.Add(this.ID, streamToWatch);
			}
			else if(e.Data.Equals("LIST"))
			{
				//Send a list of streams to the client.
				JArray streams = new JArray();
				foreach(Stream stream in StreamQueueManager.GetInstance().Streams.Values)
				{
					JObject obj = new JObject();
					obj.Add("id", stream.ClientID);
					streams.Add(obj);
				}

				this.Send(streams.ToString());
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			Clients.Remove(this.ID);
		}
	}
}
