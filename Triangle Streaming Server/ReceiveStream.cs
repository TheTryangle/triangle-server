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
using System.Configuration;

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
			var path = ConfigurationManager.AppSettings["privateKeyPath"];
			using (var reader = File.OpenText(path))
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
			if(e.Data.StartsWith("WATCH "))
			{
				//Strip the "watch" part from the string, leaving just the ID.
				//Example: "WATCH {ID}" becomes "{ID}".
				string streamToWatch = e.Data.Replace("WATCH ", "");

				//If the client is already watching a stream, remove client from stream first.
				if (Clients.ContainsKey(this.ID))
				{
					Clients.Remove(this.ID);
				}

				Clients.Add(this.ID, streamToWatch);
			}
			else if(e.Data.StartsWith("CHALLENGE: "))
			{
				string challengeString = e.Data.Replace("CHALLENGE: ", "");

				//Convert base64 string back to encrypted bytes
				var challengeBytes = Convert.FromBase64String(challengeString);

				//Decrypt bytes into string
				IAsymmetricBlockCipher eng = new Pkcs1Encoding(new RsaEngine());
				eng.Init(false, _keyPair.Private);
				var responseString = Encoding.UTF8.GetString(eng.ProcessBlock(challengeBytes, 0, challengeBytes.Length));

				//Send original message back to client
				this.Send(String.Format("CHALLENGERESPONSE: {0}", responseString));
			}
			else
			{
				switch(e.Data)
				{
					case "PUBKEY":
						//Send public key to client
						Send(_pubKey);
						break;
					case "LIST":
						//Send a list of streams to the client.
						var streams = JsonConvert.SerializeObject(StreamQueueManager.GetInstance().Streams.Values.ToList());

						this.Send(streams.ToString());
						break;
					default:
						Console.WriteLine("Unknown command: {0}", e.Data);
						break;
				}
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			Clients.Remove(this.ID);
		}
	}
}
