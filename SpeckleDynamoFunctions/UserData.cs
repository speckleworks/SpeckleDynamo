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
    public const string typeException = "UserData can only be exist on Geometry or Mesh objects. Input type: {0}.";

    /// <summary>
    /// Defines custom user properties to the input geometry
    /// </summary>
    /// <param name="geometry">Geometry</param>
    /// <param name="dictionary">User defined properties</param>
    /// <returns name="geometry">Geometry with user defined properties.</returns>
    [NodeName("Set")]
    [NodeCategory("Speckle.UserData")]
    [NodeDescription("Defines custom user properties to the input geometry")]
    public static object Set (object geometry, [DefaultArgument("{}")]DesignScript.Builtin.Dictionary dictionary)
    {
      if (geometry == null) { throw new ArgumentNullException("geometry"); }
      if (!(geometry is DesignScriptEntity))
      {
        throw new ArgumentException(String.Format(typeException, geometry.GetType()), "geometry");
      }
      if (dictionary == null) { throw new ArgumentNullException("dictionary"); }
      
      if(dictionary.Count > 0)
      {
        DesignScriptEntity newGeo;
        if (geometry is Mesh)
        {
          Mesh inputMesh = geometry as Mesh;
          newGeo = Mesh.ByPointsFaceIndices(inputMesh.VertexPositions, inputMesh.FaceIndices);
        }
        else
        {
          Geometry inputGeometry = geometry as Geometry;
          newGeo = inputGeometry.Translate();
        }
        newGeo.Tags.AddTag(speckleKey, dictionary);
        return newGeo;
      }
      else
      {
        return geometry;
      }
    }

    /// <summary>
    /// Returns a Dictionary with the custom user properties attached to the geometry, if any.
    /// </summary>
    /// <param name="geometry">Geometry</param>
    /// <returns name="dictionary">Dictionary with custom properties. Null if no property found on the geometry.</returns>
    [NodeDescription("Returns a Dictionary with the custom user properties attached to the geometry, if any.")]
    public static Dictionary Get(object geometry)
    {
      if (geometry == null) { throw new ArgumentNullException("geometry"); }
      if (!(geometry is DesignScriptEntity))
      {
        throw new ArgumentException(String.Format(typeException, geometry.GetType()), "geometry");
      }

      DesignScriptEntity dsEntity = geometry as DesignScriptEntity;
      var dict = (Dictionary)dsEntity.Tags.LookupTag(speckleKey);
      if (dict == null)
      {
        return null;
      }
      else
      {
        return dict;
      }
    }
  }
}
