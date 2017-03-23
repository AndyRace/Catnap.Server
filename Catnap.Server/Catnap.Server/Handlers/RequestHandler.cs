using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catnap.Server.Handlers
{
    public abstract class RequestHandler
    {
        public abstract Task<HttpResponseBase> Handle(HttpRequest request);
    }
}
