using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Triangle_Streaming_Server.WebApi
{
    public class StreamController : ApiController
    {
        // GET api/values 
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
        
        public void Send(int id, byte[] data)
        {

        }
    }
}
