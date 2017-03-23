using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Catnap.Server
{
  public class HttpBinaryResponse : HttpResponseBase
  {
    protected override byte[] GetBodyArray()
    {
      return Body;
    }

    protected byte[] Body { get; set; }

    public HttpBinaryResponse(HttpStatusCode statusCode)
        : base(statusCode)
    {
    }
  }

  public class HttpResponse : HttpResponseBase
  {
    public string Content { get; protected set; } = string.Empty;

    protected HttpResponse() { }

    public HttpResponse(HttpStatusCode statusCode)
        : base(statusCode)
    {
    }

    public HttpResponse(HttpStatusCode statusCode, string content)
      : base(statusCode)
    {
      Content = content;
    }

    public HttpResponse(HttpStatusCode statusCode, Dictionary<string, string> headers, string content)
        : this(statusCode, content)
    {
      Headers = headers;
    }

    protected override byte[] GetBodyArray()
    {
      return Encoding.UTF8.GetBytes(Content);
    }
  }

  public abstract class HttpResponseBase
  {
    public HttpStatusCode StatusCode { get; protected set; } = HttpStatusCode.Ok;
    public Dictionary<string, string> Headers { get; protected set; } = new Dictionary<string, string>();

    public HttpResponseBase()
    {
    }

    public HttpResponseBase(HttpStatusCode statusCode)
    {
      StatusCode = statusCode;
    }

    protected abstract byte[] GetBodyArray();

    public async Task WriteToStream(Stream stream)
    {
      var body = GetBodyArray() ?? new byte[0];
      var contentStream = new MemoryStream(body);

      var headerBuilder = new StringBuilder();
      headerBuilder.AppendLine($"HTTP/1.1 {(int)StatusCode} {StatusCode}");
      headerBuilder.AppendLine("Server: catnap-srv/1.0.0");

      foreach (var header in Headers)
      {
        headerBuilder.AppendLine($"{header.Key}: {header.Value}");
      }

      headerBuilder.AppendLine($"Content-Length: {contentStream.Length}");
      headerBuilder.AppendLine("Connection: close");
      headerBuilder.AppendLine();

      var headerArray = Encoding.UTF8.GetBytes(headerBuilder.ToString());
      await stream.WriteAsync(headerArray, 0, headerArray.Length);
      await contentStream.CopyToAsync(stream);
      await stream.FlushAsync();
    }
  }
}