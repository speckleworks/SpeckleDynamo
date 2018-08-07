using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using DesignScript.Builtin;
using Dynamo.Graph.Nodes;

namespace SpeckleDynamo.Data
{
  public static class UserData
  {
    public const string speckleKey = "speckle";

    /// <summary>
    /// Defines custom user properties to the input geometry
    /// </summary>
    /// <param name="geometry">Geometry</param>
    /// <param name="dictionary">User defined properties</param>
    /// <returns name="geometry">Geometry with user defined properties.</returns>
    [NodeName("Set")]
    [NodeCategory("Speckle.UserData")]
    [NodeDescription("Defines custom user properties to the input geometry")]
    public static Geometry Set (Geometry geometry, DesignScript.Builtin.Dictionary dictionary)
    {
      if (geometry == null) { throw new ArgumentNullException("geometry"); }
      if (dictionary == null) { throw new ArgumentNullException("dictionary"); }

      var dict = new Dictionary<string, object>();
      var dsDict = geometry.Tags.LookupTag(speckleKey);
      dictionary.Keys.ToList().ForEach(k => dict.Add(k, dictionary.ValueAtKey(k)));

      Geometry newGeo = geometry.Translate();
      newGeo.Tags.AddTag(speckleKey, dict);
      return newGeo;
    }

    /// <summary>
    /// Returns a Dictionary with the custom user properties attached to the geometry, if any.
    /// </summary>
    /// <param name="geometry">Geometry</param>
    /// <returns name="dictionary">Dictionary with custom properties. Null if no property found on the geometry.</returns>
    [NodeDescription("Returns a Dictionary with the custom user properties attached to the geometry, if any.")]
    public static DesignScript.Builtin.Dictionary Get(Geometry geometry)
    {
      if (geometry == null) { throw new ArgumentNullException("geometry"); }
      var dict = (Dictionary<string,object>)geometry.Tags.LookupTag(speckleKey);
      if(dict == null)
      {
        return null;
      }
      else
      {
        return DesignScript.Builtin.Dictionary.ByKeysValues(dict.Keys.ToList(), dict.Values.ToList());
      }
    }
  }
}
