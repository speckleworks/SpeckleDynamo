using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using DesignScript.Builtin;

namespace SpeckleDynamo
{
  public class MetaObject
  {
    [IsVisibleInDynamoLibrary(false)]
    public Dictionary<string, object> _properties;

    public object Object { get; private set; }
    public DesignScript.Builtin.Dictionary Properties
    {
      get
      {
        return DesignScript.Builtin.Dictionary.ByKeysValues(_properties.Keys.ToList(), _properties.Values.ToList());
      }
    }

    private MetaObject(object obj, Dictionary<string, object> prop)
    {
      Object = obj;
      _properties = prop;
    }

    public static MetaObject ByObjectAndDictionary(object @object, DesignScript.Builtin.Dictionary properties)
    {
      var dict = new Dictionary<string, object>();
      properties.Keys.ToList().ForEach(k => dict.Add(k, properties.ValueAtKey(k)));

      return new MetaObject(@object, dict);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static MetaObject ByObjectAndDictionaryInternal(object @object, Dictionary<string, object> properties)
    {
      return new MetaObject(@object, properties);
    }


    public override string ToString()
    {
      return String.Format("MetaObject(Object: {0}, Properties: {1})", Object.ToString(), _properties.Count());
    }

    //[IsVisibleInDynamoLibrary(false)]
    //public void Tessellate(IRenderPackage package, TessellationParameters parameters)
    //{
    //  if(Object is Geometry)
    //  {
    //    Geometry geo = (Geometry)Object;
    //    geo.Tessellate(package, parameters);
    //  }
    //}
  }

  public static class UserData
  {
    private const string speckleKey = "speckle";

    public static Geometry Set (Geometry geometry, DesignScript.Builtin.Dictionary dictionary)
    {
      var dict = new Dictionary<string, object>();
      var dsDict = geometry.Tags.LookupTag(speckleKey);
      dictionary.Keys.ToList().ForEach(k => dict.Add(k, dictionary.ValueAtKey(k)));

      if(dsDict == null)
      {
        geometry.Tags.AddTag(speckleKey, dict);
      }
      else
      {
        dsDict = dict;
      }

      return geometry;
    }

    public static DesignScript.Builtin.Dictionary Get(Geometry geometry)
    {
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
