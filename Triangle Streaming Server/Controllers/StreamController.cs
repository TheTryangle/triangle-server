using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using TriangleStreamingServer.Models;
using System.Net.Http;
using Org.BouncyCastle.Crypto;
using System.Linq;

namespace TriangleStreamingServer.Controllers
{
	//[Produces("application/json")]
	[Route("api/Stream")]
	public class StreamController : Controller
	{
		private StreamQueueManager streamQueueManager;

		public StreamController(StreamQueueManager streamQueueManager)
		{
			this.streamQueueManager = streamQueueManager;
		}

		[HttpGet]
		[Route("Connect")]
		public Guid Connect()
		{
			Guid identity = Guid.NewGuid();
			streamQueueManager.Streams.TryAdd(identity.ToString(), new Models.Stream(identity.ToString()));
			return identity;
		}

		[HttpPut]
		[Route("SendKey/{id?}")]
		public IActionResult SendKey(Guid id, [FromBody]PublicKeyModel publicKey)
		{
			TextReader textReader = new StringReader(publicKey.PublicKey);
			Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(textReader);
			AsymmetricKeyParameter publicKeyParam = (AsymmetricKeyParameter)pemReader.ReadObject();
			streamQueueManager.Streams[id.ToString()].PublicKey = publicKeyParam;
			streamQueueManager.Streams[id.ToString()].StreamerName = publicKey.StreamerName;
			return Ok();
		}

		[HttpPut]
		[Route("Send/{id?}")]
		public async Task<IActionResult> Send(Guid id)
		{
			StreamContent sc = new StreamContent(HttpContext.Request.Body);
			var result = await sc.ReadAsByteArrayAsync();
			streamQueueManager.AddToQueue(id.ToString(), result);
			return Ok();
		}

		[HttpGet]
		[Route("GetViewers/{id?}")]
		public int GetViewers(Guid id)
		{
			return streamQueueManager.ReceivingWebSocket.Clients.Where(x => x.Value.Equals(id.ToString())).Count();			
		}

		public struct PublicKeyModel
		{
			public string PublicKey { get; set; }
			public string StreamerName { get; set; }
		}
	}
}
