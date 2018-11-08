using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleDynamoExtension.Api
{

  public class AppVeyorBuild
  {
    public int buildId { get; set; }
    public int projectId { get; set; }
    public int buildNumber { get; set; }
    public string version { get; set; }
    public string message { get; set; }
    public string messageExtended { get; set; }
    public string branch { get; set; }
    public bool isTag { get; set; }
    public string commitId { get; set; }
    public string authorName { get; set; }
    public string authorUsername { get; set; }
    public string committerName { get; set; }
    public string committerUsername { get; set; }
    public DateTime committed { get; set; }
    public List<object> messages { get; set; }
    public string status { get; set; }
    public DateTime started { get; set; }
    public DateTime finished { get; set; }
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
  }
}
