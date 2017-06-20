using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using TriangleStreamingServer.Models;
using System.Net.Http;
using Org.BouncyCastle.Crypto;
using System.Text;

namespace TriangleStreamingServer.Controllers
{
    //[Produces("application/json")]
    //[Route("api/Stream")]
    public class StreamController : Controller
    {
        private StreamQueueManager streamQueueManager;

        public StreamController(StreamQueueManager streamQueueManager)
        {
            this.streamQueueManager = streamQueueManager;
        }

        [HttpGet]
        [Route("api/Stream/Connect")]
        public Guid Connect()
        {
            Guid identity = Guid.NewGuid();            
            streamQueueManager.Streams.TryAdd(identity.ToString(), new Models.Stream(identity.ToString()));
            return identity;
        }

        [HttpPut]
        [Route("api/Stream/SendKey/{id?}")]        
        public IActionResult SendKey(Guid id, [FromBody]key publicKey)
        {            
            TextReader textReader = new StringReader(publicKey.PublicKey);
            Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(textReader);
            AsymmetricKeyParameter publicKeyParam = (AsymmetricKeyParameter)pemReader.ReadObject();
            streamQueueManager.Streams[id.ToString()].PublicKey = publicKeyParam;
            return Ok();
        }

        [HttpPut]
        [Route("api/Stream/Send/{id?}")]
        public async Task<IActionResult> Send(Guid id)
        {
            StreamContent sc = new StreamContent(HttpContext.Request.Body);
            var result = await sc.ReadAsByteArrayAsync();  
            streamQueueManager.AddToQueue(id.ToString(), result);
            return Ok();
        }

        public struct key
        {
            public string PublicKey { get; set; }
        }
    }
}