using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpeckleDynamoExtension.Api
{
  public static class Api
  {
    public static async Task CheckForUpdates()
    {
      try
      {
        using (var client = new HttpClient())
        {
          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          // get the latest build on master
          using (var response = await client.GetAsync("https://ci.appveyor.com/api/projects/SpeckleWorks/speckledynamo/branch/master"))
          {
            response.EnsureSuccessStatusCode();

            var appVeyor = JsonConvert.DeserializeObject<AppVeyor>(await response.Content.ReadAsStringAsync());
            var latest = Version.Parse(appVeyor.build.version);
            var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            if (current.CompareTo(latest) < 0)
            {
              MessageBox.Show("Version " + appVeyor.build.version + " of Speckle for Dynamo is available:" + "\n\n" + appVeyor.build.messageExtended, "Update available", MessageBoxButton.OK, MessageBoxImage.Information);
            }
          }
        }
      }
      catch(Exception e)
      {
        Debug.Write("failed silently");
      }
    }
  }
}
