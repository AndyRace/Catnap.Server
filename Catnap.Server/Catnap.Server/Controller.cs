using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web;
using Windows.Web.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.IO;
using Catnap.Server.Util;
using System.Text.RegularExpressions;

namespace Catnap.Server
{
  public abstract class Controller
  {
    public string Prefix = "";
    public List<MethodInfo> RoutingMethods;
    public List<MethodInfo> DefaultMethods;

    public Controller()
    {
      var type = this.GetType().GetTypeInfo();
      var routePrefix = (RoutePrefix)this.GetType().GetTypeInfo().GetCustomAttribute(typeof(RoutePrefix));

      if (routePrefix == null) throw new Exception("Please add a RoutePrefix attribute to your Controller");

      Prefix = routePrefix.Path;

      var methods = GetType().GetMethods().ToList();
      RoutingMethods = methods.Where(m => m.GetCustomAttribute(typeof(Route)) != null).ToList();
      DefaultMethods = methods.Where(m => m.GetCustomAttribute(typeof(DefaultRoute)) != null).ToList();
    }

    public async Task<HttpResponseBase> Handle(HttpRequest request)
    {
      var url = request.Path;
      var httpMethod = request.Method;
      var requestPath = Uri.UnescapeDataString(url.AbsolutePath);

      foreach (var route in RoutingMethods)
      {
        var routingMethod = ((HttpRequestMethod)route.GetCustomAttribute(typeof(HttpRequestMethod)))?.Method ?? HttpMethod.Get;
        var routingPath = RESTPath.Combine(Prefix, ((Route)route.GetCustomAttribute(typeof(Route))).Path);

        bool sameMethod = String.Equals(routingMethod.Method, httpMethod.Method);
        bool matchingUrl = routingPath.PathReMatch.IsMatch(requestPath);

        if (sameMethod && matchingUrl)
        {
          var method = route;
          var parameters = ExtractParameters(method, routingPath, requestPath, request);

          if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            return await (Task<HttpResponseBase>)method.Invoke(this, parameters.ToArray());
          else
            return (HttpResponseBase)method.Invoke(this, parameters.ToArray());
        }
      }

      foreach (var route in DefaultMethods)
      {
        var routingMethod = ((HttpRequestMethod)route.GetCustomAttribute(typeof(HttpRequestMethod)))?.Method ?? HttpMethod.Get;
        bool sameMethod = String.Equals(routingMethod.Method, httpMethod.Method);

        if (sameMethod)
        {
          var method = route;

          if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            return await (Task<HttpResponseBase>)method.Invoke(this, new[] { requestPath });
          else
            return (HttpResponseBase)method.Invoke(this, new[] { requestPath });
        }
      }

      return NotFound($"Couldn't find a fitting method on the on matched controller '{ GetType().Name }' for path '{ url }'");
    }

    private List<object> ExtractParameters(MethodInfo method, RESTPath path, string requestPath, HttpRequest request)
    {
      var parameters = new List<object>();
      var methodParams = method.GetParameters();
      var requestParams = path.PathReMatch.Matches(requestPath)[0];

      foreach (var param in methodParams)
      {
        if (param.GetCustomAttribute(typeof(Body)) != null)
          parameters.Add(request.Content);
        else
          parameters.Add(requestParams.Groups[param.Name].Value);
      }

      return parameters;
    }

    public HttpResponse BadRequest(string message = "")
    {
      return new HttpResponse(HttpStatusCode.BadRequest, message);
    }

    public HttpResponse NotFound(string message = "")
    {
      return new HttpResponse(HttpStatusCode.NotFound, message);
    }
  }
}
