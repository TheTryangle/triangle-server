using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Triangle_Streaming_Server.WebApi
{
    public class StreamController : ApiController
    {
        // Test GET
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet]
        public Guid Connect()
        {
            Guid identity = Guid.NewGuid();
            StreamQueueManager.GetInstance().Streams.Add(identity.ToString(), new Stream(identity.ToString()));
            return identity;
        }
        
        [HttpPut]
        public async Task<IHttpActionResult> Send(Guid id)
        {
            var file = await Request.Content.ReadAsByteArrayAsync();
            StreamQueueManager.GetInstance().AddToQueue(id.ToString(), file);
            return Ok();
        }
    }
}
