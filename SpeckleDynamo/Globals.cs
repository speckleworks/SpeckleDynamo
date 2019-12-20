using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;

namespace SpeckleDynamo
{
  public static class Globals
  {
    public static ObservableCollection<SpeckleStream> UserStreams { get; set; }
    public static DateTime LastCheckedStreams { get; set; }
    public static ObservableCollection<Project> UserProjects { get; set; }
    public static DateTime LastCheckedProjects { get; set; }
  }
}
