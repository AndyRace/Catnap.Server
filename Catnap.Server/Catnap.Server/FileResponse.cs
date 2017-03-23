using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Web.Http;
using System.IO;

namespace Catnap.Server
{
  public class FileResponse : HttpBinaryResponse
  {
    private static Dictionary<string, string> _map = new Dictionary<string, string>()
      {
        {".js", "application/javascript" },
        {".xml", "application/xml" },
        {".zip", "application/zip" },
        {".pdf", "application/pdf" },
        {".mpg", "audio/mpeg" },
        {".mpeg", "audio/mpeg" },
        {".css", "text/css" },
        {".html", "text/html" },
        {".png", "image/png" },
        {".jpg", "image/jpeg" },
        {".jpeg", "image/jpeg" },
        {".gif", "image/gif" },
        {".ico", "image/x-icon" }
      };

    // see: https://en.wikipedia.org/wiki/Media_type
    private string GetContentType(string path)
    {
      var ext = Path.GetExtension(path).ToLowerInvariant();
      if (_map.TryGetValue(ext, out string contentType))
        return contentType;
      return "text/plain";
    }

    public FileResponse(String path, HttpStatusCode statusCode = HttpStatusCode.Ok)
        : base(statusCode)
    {
      try
      {
        //this.Content = Encoding.UTF8.GetString(File.ReadAllBytes(path));

        // restrict which file paths can be used
        if (Path.IsPathRooted(path)) throw new FileNotFoundException();

        this.Body = File.ReadAllBytes(path);
        this.Headers.Add("Content-Type", GetContentType(path));

        // It is the responsibility of the client to indicate which format it 'Accept's
        // this.Headers.Add("Accept", "application/json");
      }
      catch (FileNotFoundException)
      {
        StatusCode = HttpStatusCode.NotFound;
      }
      catch
      {
        // todo: Match exception to status code
        this.StatusCode = HttpStatusCode.NotFound;
      }
    }
  }
}
