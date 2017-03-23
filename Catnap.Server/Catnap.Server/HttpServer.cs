﻿using Catnap.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.Web.Http;

namespace Catnap.Server
{
  public sealed class HttpServer : IDisposable
  {
    private readonly StreamSocketListener listener;
    private readonly int port;
    public RESTHandler RestHandler { get; } = new RESTHandler();

    private List<String> AcceptedVerbs = new List<String> { HttpMethod.Get.Method, HttpMethod.Post.Method,
                                                                HttpMethod.Delete.Method, HttpMethod.Put.Method };

    public HttpServer(int serverPort = 1337)
    {
      listener = new StreamSocketListener();
      port = serverPort;
      listener.ConnectionReceived += async (s, e) => await ThreadPool.RunAsync(async (w) =>
      {
        var socket = e.Socket;
        try
        {
          await ProcessRequestAsync(socket);
        }
        catch (Exception ex)
        {
          try
          {
            await WriteInternalServerErrorResponse(socket, ex);
          }
          catch (Exception)
          {
            Debug.WriteLine("ERROR: Unable to write error response!");
          }
        }
        finally
        {
          await socket.CancelIOAsync();
          socket.Dispose();
        }
      });
    }

    public async Task StartServerAsync()
    {
      await listener.BindServiceNameAsync(port.ToString());
    }

    private async Task ProcessRequestAsync(StreamSocket socket)
    {
      HttpRequest request;
      request = HttpRequest.Read(socket);

      if (AcceptedVerbs.Contains(request.Method.Method))
      {
        HttpResponseBase response;
        response = await RestHandler.Handle(request);
        await WriteResponse(response, socket);
      }
    }

    private static async Task WriteInternalServerErrorResponse(StreamSocket socket, Exception ex)
    {
      var httpResponse = GetInternalServerError(ex);
      await WriteResponse(httpResponse, socket);
    }

    private static HttpResponse GetInternalServerError(Exception exception)
    {
      var errorMessage = "Internal server error occurred.";
      if (Debugger.IsAttached)
        errorMessage += Environment.NewLine + exception;

      var httpResponse = new HttpResponse(HttpStatusCode.InternalServerError, errorMessage);
      return httpResponse;
    }

    private static async Task WriteResponse(HttpResponseBase response, StreamSocket socket)
    {
      var output = socket.OutputStream;
      using (var stream = output.AsStreamForWrite())
      {
        await response.WriteToStream(stream);
      }
    }

    public void Dispose()
    {
      listener.Dispose();
    }
  }
}