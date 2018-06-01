using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleDynamo.Utils
{
  internal static class Accounts
  {
    internal static string GetAuthToken(string email, string restApi)
    {
      string strPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\SpeckleSettings";
      if (Directory.Exists(strPath) && Directory.EnumerateFiles(strPath, "*.txt").Count() > 0)
        foreach (string file in Directory.EnumerateFiles(strPath, "*.txt"))
        {
          string content = File.ReadAllText(file);
          string[] pieces = content.TrimEnd('\r', '\n').Split(',');

          try
          {
            if (pieces[0] == email && pieces[3] == restApi)
              return pieces[1];
          }
          catch (Exception e)
          {
            return "";
          }
        }
      return "";
    }
  }
}
