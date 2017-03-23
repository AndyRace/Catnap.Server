using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Catnap.Server.Util
{
  public class RESTPath
  {
    public Regex PathReMatch { get; private set; }

    private Dictionary<int, string> Parameters = new Dictionary<int, string>();

    private RESTPath()
    {
    }

    public static RESTPath Combine(params string[] paths)
    {
      // trim "/"s from the paths, re-combine into an array ready to join with "/" delimiters
      var newPath = string.Join("/", paths.Select(path => path.Trim('/')).ToArray());

      // replace "{" <param name> "}" with regex pattern used for matching parameters
      return new RESTPath()
      {
        // (?<name>pattern)
        PathReMatch = new Regex("^" + Regex.Replace(newPath, "{([^}]*)}", "(?<$1>[\\w\\.-]+)") + "/?$")
      };
    }

    public bool Matches(String path)
    {
      return PathReMatch.IsMatch(path);
    }
  }
}
