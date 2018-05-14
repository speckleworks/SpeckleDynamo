using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleDynamo.Functions
{
  /// <summary>
  /// AssociativeNodes do not support objects, this is a dirty workaround to pass data from
  /// this project to the Functions dll in order to output it correctly
  /// In the future could be replaced with a local caching DB
  /// </summary>
  public static class SpeckleTempData
  {
    private static Dictionary<string, object> SpeckleLayers = new Dictionary<string, object>();

    public static object GetLayerObjects(string layer)
    {
      if (SpeckleLayers.ContainsKey(layer))
        return SpeckleLayers[layer];
      return null;
    }

    public static void AddLayerObjects(string layer, object objects)
    {
      SpeckleLayers[layer] = objects;
    }
  }
}
