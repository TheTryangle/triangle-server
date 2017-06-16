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
            var provider = new MultipartMemoryStreamProvider();

            await Request.Content.ReadAsMultipartAsync(provider);

            if (provider.Contents.Count == 0) return InternalServerError(new Exception("Upload failed"));

            var file = provider.Contents[0]; // if you handle more then 1 file you can loop provider.Contents

            var buffer = await file.ReadAsByteArrayAsync();

            // .. do whatever needed here
            StreamQueueManager.GetInstance().AddToQueue(id.ToString(), buffer);


            return Ok();

        }
    }
}
