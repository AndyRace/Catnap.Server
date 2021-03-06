﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using Catnap.Server.Util;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Catnap.Server
{
  public abstract class Controller
  {
    public string Prefix = "";
    public List<MethodInfo> RoutingMethods, DefaultMethods, PreMethods, PostMethods;

    public Controller()
    {
      var type = GetType().GetTypeInfo();
      var routePrefix = (RoutePrefix)this.GetType().GetTypeInfo().GetCustomAttribute(typeof(RoutePrefix));

      if (routePrefix == null) throw new Exception("Please add a RoutePrefix attribute to your Controller");

      Prefix = routePrefix.Path;

      var methods = GetType().GetMethods().ToList();
      RoutingMethods = methods.Where(m => m.GetCustomAttribute(typeof(Route)) != null).ToList();
      DefaultMethods = methods.Where(m => m.GetCustomAttribute(typeof(DefaultRoute)) != null).ToList();
      PreMethods = methods.Where(m => m.GetCustomAttribute(typeof(PreRoute)) != null).ToList();
      PostMethods = methods.Where(m => m.GetCustomAttribute(typeof(PostRoute)) != null).ToList();
    }

    public async Task<HttpResponseBase> Handle(HttpRequest request)
    {
      var url = request.Path;
      var httpMethod = request.Method;
      var requestPath = Uri.UnescapeDataString(url.AbsolutePath);

      foreach (var method in PreMethods)
      {
        var parameters = new object[] { request };

        // Debug.WriteLine($"{requestPath}: Calling pre-execution method ({method.Name})");
        if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
          await (Task)method.Invoke(this, parameters);
        else
          method.Invoke(this, parameters);
      }

      try
      {
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

            Debug.WriteLine($"{requestPath}: Calling handler method ({method.Name})");
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

            // Debug.WriteLine($"{requestPath}: Calling default handler method ({method.Name})");
            if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
              return await (Task<HttpResponseBase>)method.Invoke(this, new[] { requestPath });
            else
              return (HttpResponseBase)method.Invoke(this, new[] { requestPath });
          }
        }

      }
      finally
      {
        foreach (var method in PostMethods)
        {
          var parameters = new object[] { request };

          // Debug.WriteLine($"{requestPath}: Calling post-execution method ({method.Name})");
          if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            await (Task)method.Invoke(this, parameters);
          else
            method.Invoke(this, parameters);
        }
      }

      return NotFound($"Couldn't find a suitable method on controller '{GetType().Name}' for path '{url}'");
    }

    private List<object> ExtractParameters(MethodInfo method, RESTPath path, string requestPath, HttpRequest request)
    {
      var parameters = new List<object>();
      var methodParams = method.GetParameters();
      var requestParams = path.PathReMatch.Matches(requestPath)[0];

      foreach (var param in methodParams)
      {
        if (param.GetCustomAttribute(typeof(JsonBody)) != null)
        {
          dynamic jsonObj = JsonConvert.DeserializeObject(request.Content);
          parameters.Add(jsonObj);
        }
        else if (param.GetCustomAttribute(typeof(Body)) != null)
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
