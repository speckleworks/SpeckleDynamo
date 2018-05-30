using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Geometry;
using Dynamo.Graph.Nodes.ZeroTouch;
using Newtonsoft.Json;

using SpeckleCore;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpeckleDynamo
{
  public class ConverterHack { /*makes sure the assembly is loaded*/  public ConverterHack() { } }

  /// <summary>
  /// These methods extend both the SpeckleObject types with a .ToNative() method as well as 
  /// the base RhinoCommon types with a .ToSpeckle() method for easier conversion logic.
  /// </summary>`
  public static class SpeckleDynamoConverter
  {
    const double EPS = 1e-6;

    public static bool SetBrepDisplayMesh = true;
    public static bool AddMeshTextureCoordinates = false;
    public static bool AddRhinoObjectProperties = false;
    public static bool AddBasicLengthAreaVolumeProperties = false;

    // Dictionaries & ArchivableDictionaries
    //public static Dictionary<string, object> ToSpeckle(this ArchivableDictionary dict)
    //{
    //  if (dict == null) return null;
    //  Dictionary<string, object> myDictionary = new Dictionary<string, object>();

    //  foreach (var key in dict.Keys)
    //  {
    //    if (dict[key] is ArchivableDictionary)
    //      myDictionary.Add(key, ((ArchivableDictionary)dict[key]).ToSpeckle());
    //    else if (dict[key] is string || dict[key] is double || dict[key] is float || dict[key] is int || dict[key] is SpeckleObject)
    //      myDictionary.Add(key, dict[key]);
    //    else if (dict[key] is IEnumerable)
    //    {
    //      //  TODO
    //    }
    //    else
    //      myDictionary.Add(key, SpeckleCore.Converter.Serialise(dict[key]));
    //  }
    //  return myDictionary;
    //}

    //public static ArchivableDictionary ToNative(this Dictionary<string, object> dict)
    //{
    //  ArchivableDictionary myDictionary = new ArchivableDictionary();
    //  if (dict == null) return myDictionary;

    //  foreach (var key in dict.Keys)
    //  {
    //    if (dict[key] is Dictionary<string, object>)
    //    {
    //      myDictionary.Set(key, ((Dictionary<string, object>)dict[key]).ToNative());
    //    }
    //    else if (dict[key] is SpeckleObject)
    //    {
    //      var converted = SpeckleCore.Converter.Deserialise((SpeckleObject)dict[key]);

    //      if (converted is GeometryBase)
    //        myDictionary.Set(key, (GeometryBase)converted);
    //      else if (converted is Interval)
    //        myDictionary.Set(key, (Interval)converted);
    //      else if (converted is Vector3d)
    //        myDictionary.Set(key, (Vector3d)converted);
    //      else if (converted is Plane)
    //        myDictionary.Set(key, (Plane)converted);
    //    }
    //    else if (dict[key] is int)
    //      myDictionary.Set(key, Convert.ToInt32(dict[key]));
    //    else if (dict[key] is double)
    //      myDictionary.Set(key, (double)dict[key]);
    //    else if (dict[key] is bool)
    //      myDictionary.Set(key, (bool)dict[key]);
    //    else if (dict[key] is string)
    //      myDictionary.Set(key, (string)dict[key]);
    //  }
    //  return myDictionary;
    //}

    // Convenience methods point:
    public static double[] ToArray(this Point pt)
    {
      return new double[] { pt.X, pt.Y, pt.Z };
    }


    public static Point ToPoint(this double[] arr)
    {
      return Point.ByCoordinates(arr[0], arr[1], arr[2]);
    }

    public static double ToDegrees(this double radians)
    {
      return radians * (180 / Math.PI);
    }

    public static double ToRadians(this double degrees)
    {
      return degrees * (Math.PI / 180);
    }

    public static bool IsLinear(this Curve curve)
    {
      //Dynamo cannot be trusted when less than 1e-6
      var extremesDistance = curve.StartPoint.DistanceTo(curve.EndPoint);
      return Math.Abs(curve.Length - extremesDistance) <= EPS;
    }

    public static bool Threshold(double value1, double value2)
    {
      return Math.Abs(value1 - value2) <= EPS;
    }

    // numbers
    public static SpeckleNumber ToSpeckle(this float num)
    {
      return new SpeckleNumber(num);
    }

    public static SpeckleNumber ToSpeckle(this long num)
    {
      return new SpeckleNumber(num);
    }

    public static SpeckleNumber ToSpeckle(this int num)
    {
      return new SpeckleNumber(num);
    }

    public static SpeckleNumber ToSpeckle(this double num)
    {
      return new SpeckleNumber(num);
    }

    public static double? ToNative(this SpeckleNumber num)
    {
      return num.Value;
    }

    // booleans 
    public static SpeckleBoolean ToSpeckle(this bool b)
    {
      return new SpeckleBoolean(b);
    }

    public static bool? ToNative(this SpeckleBoolean b)
    {
      return b.Value;
    }

    // strings
    public static SpeckleString ToSpeckle(this string b)
    {
      return new SpeckleString(b);
    }

    public static string ToNative(this SpeckleString b)
    {
      return b.Value;
    }


    // Mass point converter
    public static Point[] ToPoints(this IEnumerable<double> arr)
    {
      if (arr.Count() % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      Point[] points = new Point[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
        points[k++] = Point.ByCoordinates(asArray[i - 2], asArray[i - 1], asArray[i]);

      return points;
    }

    public static double[] ToFlatArray(this IEnumerable<Point> points)
    {
      return points.SelectMany(pt => pt.ToArray()).ToArray();
    }

    // Convenience methods vector:
    public static double[] ToArray(this Vector vc)
    {
      return new double[] { vc.X, vc.Y, vc.Z };
    }

    public static Vector ToVector(this double[] arr)
    {
      return Vector.ByCoordinates(arr[0], arr[1], arr[2]);
    }

    // The real deals below: 

    // Points
    // GhCapture?
    public static SpecklePoint ToSpeckle(this Point pt)
    {
      return new SpecklePoint(pt.X, pt.Y, pt.Z);
    }
    // Rh Capture?
    public static Point ToNative(this SpecklePoint pt)
    {
      var myPoint = Point.ByCoordinates(pt.Value[0], pt.Value[1], pt.Value[2]);
      //TODO: handle user dictionaries
      //myPoint.UserDictionary.ReplaceContentsWith(pt.Properties.ToNative());
      return myPoint;
    }

    // Vectors
    public static SpeckleVector ToSpeckle(this Vector pt)
    {
      return new SpeckleVector(pt.X, pt.Y, pt.Z);
    }

    public static Vector ToNative(this SpeckleVector pt)
    {
      return Vector.ByCoordinates(pt.Value[0], pt.Value[1], pt.Value[2]);
    }

    //TODO: interval
    // Interval
    //public static SpeckleInterval ToSpeckle(this Interval interval)
    //{
    //  return new SpeckleInterval(interval.T0, interval.T1);
    //}

    //public static Interval ToNative(this SpeckleInterval interval)
    //{
    //  return new Interval((double)interval.Start, (double)interval.End); ;
    //}

    // Interval2d
    //public static SpeckleInterval2d ToSpeckle(this UVInterval interval)
    //{
    //  return new SpeckleInterval2d(interval.U.ToSpeckle(), interval.V.ToSpeckle());
    //}

    //public static UVInterval ToNative(this SpeckleInterval2d interval)
    //{
    //  return new UVInterval(interval.U.ToNative(), interval.V.ToNative());
    //}

    // Plane
    public static SpecklePlane ToSpeckle(this Plane plane)
    {
      return new SpecklePlane(plane.Origin.ToSpeckle(), plane.Normal.ToSpeckle(), plane.XAxis.ToSpeckle(), plane.YAxis.ToSpeckle());
    }

    public static Plane ToNative(this SpecklePlane plane)
    {
      var returnPlane = Plane.ByOriginNormal(plane.Origin.ToNative(), plane.Normal.ToNative());
      return returnPlane;
    }

    // #region LifeSucks

    // Line
    public static SpeckleLine ToSpeckle(this Line line)
    {
      return new SpeckleLine((new Point[] { line.StartPoint, line.EndPoint }).ToFlatArray());
    }

    // Rh Line capture
    //public static SpeckleLine ToSpeckle(this LineCurve line)
    //{
    //  return new SpeckleLine((new Point3d[] { line.PointAtStart, line.PointAtEnd }).ToFlatArray(), properties: line.UserDictionary.ToSpeckle());
    //}

    public static Line ToNative(this SpeckleLine line)
    {
      var pts = line.Value.ToPoints();
      var myLine = Line.ByStartPointEndPoint(pts[0], pts[1]);
      // myLine.UserDictionary.ReplaceContentsWith(line.Properties.ToNative());
      return myLine;
    }


    // Circle
    public static SpeckleCircle ToSpeckle(this Circle circ)
    {
      return new SpeckleCircle(circ.CenterPoint.ToSpeckle(), circ.Normal.ToSpeckle(), circ.Radius);
    }

    public static Circle ToNative(this SpeckleCircle circ)
    {
      return Circle.ByCenterPointRadiusNormal(circ.Center.ToNative(), (double)circ.Radius, circ.Normal.ToNative());
      //var myCircle = new Arc(circle);
      //myCircle.UserDictionary.ReplaceContentsWith(circ.Properties.ToNative());
      //return myCircle;
    }

    // Arc
    public static SpeckleArc ToSpeckle(this Arc a)
    {
        SpeckleArc arc = new SpeckleArc(
                Plane.ByOriginNormal(a.CenterPoint, a.Normal).ToSpeckle(),
                a.Radius,
                a.StartAngle.ToRadians(),
                (a.StartAngle + a.SweepAngle).ToRadians(),
                a.SweepAngle.ToRadians()
            );
        return arc;
    }

    public static Arc ToNative(this SpeckleArc a)
    {
        Arc arc = Arc.ByCenterPointRadiusAngle(
                a.Plane.Origin.ToNative(),
                a.Radius.Value,
                a.StartAngle.Value.ToDegrees(),
                a.EndAngle.Value.ToDegrees(),
                a.Plane.Normal.ToNative()
        );
        return arc;
    }

    //Ellipse
    public static SpeckleEllipse ToSpeckle(this Ellipse e)
    {
        return new SpeckleEllipse(
            Plane.ByOriginNormal(e.CenterPoint, e.Normal).ToSpeckle(),
            e.MajorAxis.Length,
            e.MinorAxis.Length
        );
    }

    public static Ellipse ToNative(this SpeckleEllipse e)
    {
        return Ellipse.ByPlaneRadii(
            e.Plane.ToNative(),
            e.FirstRadius.Value,
            e.SecondRadius.Value
        );
    }

    
    /// <summary>
    /// Dynamo only has polycurves, so if all curves are lines
    /// returns SpecklePolyline, SpecklePolycurve otherwise
    /// </summary>
    /// <param name="polycurve"></param>
    /// <returns name="speckleObject"></returns>
    public static SpeckleObject ToSpeckle(this PolyCurve polycurve)
    {
      bool isPolyline = polycurve.Curves().All(c => c.IsLinear());

      if (isPolyline)
      {
        var points = polycurve.Curves().Select(c => c.StartPoint).ToList();
        points.Add(polycurve.Curves().Last().EndPoint);
        return new SpecklePolyline(points.ToFlatArray());
      }
      else
      {
        throw new NotImplementedException("Curves not implemented just yet!");
      }
    }

    /// <summary>
    /// Dynamo polygon converter to SpecklePolyline
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns></returns>
    public static SpecklePolyline ToSpeckle(this Polygon polygon)
    {
      return new SpecklePolyline(polygon.Points.ToFlatArray())
      {
        Closed = true
      };
    }

    public static SpecklePolyline ToSpeckle(this Rectangle rect)
    {
      return (rect as Polygon).ToSpeckle();
    }

    /// <summary>
    /// If SpecklePolyline is closed returns polygon (rectangle if 4 points and parallel sides), polycurve otherwise
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    public static Curve ToNative(this SpecklePolyline polyline)
    {
      var points = polyline.Value.ToPoints();
      var polycurve = PolyCurve.ByPoints(polyline.Value.ToPoints());
      // If closed and planar, make polygon
      if (polyline.Closed && polycurve.IsPlanar)
      {
        polycurve.Dispose(); // geometry not needed. Freeing memory.
        double dot = Vector.ByTwoPoints(points[0], points[1]).Dot(Vector.ByTwoPoints(points[1], points[2]));

        if(points.Count() == 4 && Threshold(dot,0))
        {
          return Rectangle.ByCornerPoints(points);
        }
        else
        {
          return Polygon.ByPoints(polyline.Value.ToPoints());
        }
      }
      else
      {
        return polycurve;
      }
    }


    //    // Polycurve
    //    // Rh Capture/Gh Capture
    //    public static SpecklePolycurve ToSpeckle(this PolyCurve p)
    //    {
    //      SpecklePolycurve myPoly = new SpecklePolycurve();

    //      p.RemoveNesting();
    //      var segments = p.Explode();

    //      myPoly.Segments = segments.Select(s => { return s.ToSpeckle(); }).ToList();
    //      myPoly.Properties = p.UserDictionary.ToSpeckle();
    //      myPoly.SetHashes(myPoly.Segments.Select(obj => obj.Hash).ToArray());
    //      return myPoly;
    //    }

    //    public static PolyCurve ToNative(this SpecklePolycurve p)
    //    {

    //      PolyCurve myPolyc = new PolyCurve();
    //      foreach (var segment in p.Segments)
    //      {
    //        if (segment.Type == SpeckleObjectType.Curve)
    //          myPolyc.Append(((SpeckleCurve)segment).ToNative());

    //        if (segment.Type == SpeckleObjectType.Line)
    //          myPolyc.Append(((SpeckleLine)segment).ToNative());

    //        if (segment.Type == SpeckleObjectType.Arc)
    //          myPolyc.Append(((SpeckleArc)segment).ToNative());

    //        if (segment.Type == SpeckleObjectType.Polyline)
    //          myPolyc.Append(((SpecklePolyline)segment).ToNative().ToNurbsCurve());
    //      }
    //      myPolyc.UserDictionary.ReplaceContentsWith(p.Properties.ToNative());
    //      return myPolyc;
    //    }

    //    public static SpeckleObject ToSpeckle(this Curve curve)
    //    {
    //      var properties = curve.UserDictionary.ToSpeckle();

    //      if (curve is PolyCurve)
    //      {
    //        return ((PolyCurve)curve).ToSpeckle();
    //      }

    //      if (curve.IsArc())
    //      {
    //        Arc getObj; curve.TryGetArc(out getObj);
    //        SpeckleArc myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      if (curve.IsCircle())
    //      {
    //        Circle getObj; curve.TryGetCircle(out getObj);
    //        SpeckleCircle myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      if (curve.IsEllipse())
    //      {
    //        Ellipse getObj; curve.TryGetEllipse(out getObj);
    //        SpeckleEllipse myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      if (curve.IsLinear() || curve.IsPolyline()) // defaults to polyline
    //      {
    //        Polyline getObj; curve.TryGetPolyline(out getObj);
    //        SpeckleObject myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      Polyline poly;
    //      curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly);

    //      SpeckleCurve myCurve = new SpeckleCurve((SpecklePolyline)poly.ToSpeckle(), properties: curve.UserDictionary.ToSpeckle());
    //      NurbsCurve nurbsCurve = curve.ToNurbsCurve();

    //      myCurve.Weights = nurbsCurve.Points.Select(ctp => ctp.Weight).ToList();
    //      myCurve.Points = nurbsCurve.Points.Select(ctp => ctp.Location).ToFlatArray().ToList();
    //      myCurve.Knots = nurbsCurve.Knots.ToList();
    //      myCurve.Degree = nurbsCurve.Degree;
    //      myCurve.Periodic = nurbsCurve.IsPeriodic;
    //      myCurve.Rational = nurbsCurve.IsRational;
    //      myCurve.Domain = nurbsCurve.Domain.ToSpeckle();

    //      myCurve.Properties = properties;
    //      return myCurve;
    //    }

    //    // Curve
    //    public static SpeckleObject ToSpeckle(this NurbsCurve curve)
    //    {
    //      var properties = curve.UserDictionary.ToSpeckle();

    //      if (curve.IsArc())
    //      {
    //        Arc getObj; curve.TryGetArc(out getObj);
    //        SpeckleArc myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      if (curve.IsCircle())
    //      {
    //        Circle getObj; curve.TryGetCircle(out getObj);
    //        SpeckleCircle myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      if (curve.IsEllipse())
    //      {
    //        Ellipse getObj; curve.TryGetEllipse(out getObj);
    //        SpeckleEllipse myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      if (curve.IsLinear() || curve.IsPolyline()) // defaults to polyline
    //      {
    //        Polyline getObj; curve.TryGetPolyline(out getObj);
    //        SpeckleObject myObject = getObj.ToSpeckle(); myObject.Properties = properties;
    //        return myObject;
    //      }

    //      Polyline poly;
    //      curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly);

    //      SpeckleCurve myCurve = new SpeckleCurve(poly: (SpecklePolyline)poly.ToSpeckle(), properties: curve.UserDictionary.ToSpeckle());

    //      myCurve.Weights = curve.Points.Select(ctp => ctp.Weight).ToList();
    //      myCurve.Points = curve.Points.Select(ctp => ctp.Location).ToFlatArray().ToList();
    //      myCurve.Knots = curve.Knots.ToList();
    //      myCurve.Degree = curve.Degree;
    //      myCurve.Periodic = curve.IsPeriodic;
    //      myCurve.Rational = curve.IsRational;
    //      myCurve.Domain = curve.Domain.ToSpeckle();

    //      myCurve.Properties = properties;
    //      return myCurve;
    //    }

    //    public static NurbsCurve ToNative(this SpeckleCurve curve)
    //    {
    //      var ptsList = curve.Points.ToPoints();

    //      // Bug/feature in Rhino sdk: creating a periodic curve adds two extra stupid points? 
    //      var myCurve = NurbsCurve.Create(curve.Periodic, curve.Degree, new Point3d[curve.Periodic ? ptsList.Length - 2 : ptsList.Length]);
    //      myCurve.Domain = curve.Domain.ToNative();

    //      for (int i = 0; i < ptsList.Length; i++)
    //        myCurve.Points.SetPoint(i, ptsList[i].X, ptsList[i].Y, ptsList[i].Z, curve.Weights[i]);

    //      for (int i = 0; i < curve.Knots.Count; i++)
    //        myCurve.Knots[i] = curve.Knots[i];

    //      return myCurve;
    //    }

    //    #endregion

    //    // do not worry (too much!) from down here onwards

    //    // Box
    //    public static SpeckleBox ToSpeckle(this Box box)
    //    {
    //      return new SpeckleBox(box.Plane.ToSpeckle(), box.X.ToSpeckle(), box.Y.ToSpeckle(), box.Z.ToSpeckle());
    //    }

    //    public static Box ToNative(this SpeckleBox box)
    //    {
    //      return new Box(box.BasePlane.ToNative(), box.XSize.ToNative(), box.YSize.ToNative(), box.ZSize.ToNative());
    //    }

    // Meshes
    public static SpeckleMesh ToSpeckle(this Mesh mesh)
    {
      var vertices = mesh.VertexPositions.ToFlatArray();
      var defaultColour = System.Drawing.Color.FromArgb(255, 100, 100, 100);

      var faces = mesh.FaceIndices.SelectMany(f =>
      {
        if(f.Count == 4) { return new int[5] { 1, (int)f.A, (int)f.B, (int)f.C, (int)f.D }; }
        else { return new int[4] {0,  (int)f.A, (int)f.B, (int)f.C}; }
      })
      .ToArray();

      var colors = Enumerable.Repeat(defaultColour.ToArgb(), vertices.Count()).ToArray();
      //double[] textureCoords;

      //if (SpeckleRhinoConverter.AddMeshTextureCoordinates)
      //{
      //  textureCoords = mesh.TextureCoordinates.Select(pt => pt).ToFlatArray();
      //  return new SpeckleMesh(vertices, faces, Colors, textureCoords, properties: mesh.UserDictionary.ToSpeckle());
      //}

      return new SpeckleMesh(vertices, faces, colors, null);
    }

    public static Mesh ToNative(this SpeckleMesh mesh)
    {
      var points = mesh.Vertices.ToPoints();
      List<IndexGroup> faces = new List<IndexGroup>();
      int i = 0;

      while (i < mesh.Faces.Count)
      {
        if (mesh.Faces[i] == 0)
        { // triangle
          var ig = IndexGroup.ByIndices((uint)mesh.Faces[i + 1], (uint)mesh.Faces[i + 2],(uint)mesh.Faces[i + 3]);
          faces.Add(ig);
          i += 4;
        }
        else
        { // quad
          var ig = IndexGroup.ByIndices((uint)mesh.Faces[i + 1], (uint)mesh.Faces[i + 2], (uint)mesh.Faces[i + 3], (uint)mesh.Faces[i + 4]);
          faces.Add(ig);
          i += 5;
        }
      }

      return Mesh.ByPointsFaceIndices(points, faces);
    }

    //    // Breps
    //    public static SpeckleBrep ToSpeckle(this Brep brep)
    //    {
    //      var joinedMesh = new Mesh();
    //      if (SpeckleRhinoConverter.SetBrepDisplayMesh)
    //      {
    //        MeshingParameters mySettings;
    //#if R6
    //      mySettings = new MeshingParameters(0);
    //#else
    //        mySettings = MeshingParameters.Coarse;

    //        mySettings.SimplePlanes = true;
    //        mySettings.RelativeTolerance = 0;
    //        mySettings.GridAspectRatio = 6;
    //        mySettings.GridAngle = Math.PI;
    //        mySettings.GridAspectRatio = 0;
    //        mySettings.SimplePlanes = true;
    //#endif

    //        Mesh.CreateFromBrep(brep, mySettings).All(meshPart => { joinedMesh.Append(meshPart); return true; });
    //      }

    //      return new SpeckleBrep(displayValue: SpeckleRhinoConverter.SetBrepDisplayMesh ? joinedMesh.ToSpeckle() : null, rawData: JsonConvert.SerializeObject(brep), provenance: "Rhino", properties: brep.UserDictionary.ToSpeckle());
    //    }

    //    public static Brep ToNative(this SpeckleBrep brep)
    //    {
    //      try
    //      {
    //        if (brep.Provenance == "Rhino")
    //        {
    //          var myBrep = JsonConvert.DeserializeObject<Brep>((string)brep.RawData);
    //          myBrep.UserDictionary.ReplaceContentsWith(brep.Properties.ToNative());
    //          return myBrep;
    //        }
    //        throw new Exception("Unknown brep provenance: " + brep.Provenance + ". Don't know how to convert from one to the other.");
    //      }
    //      catch
    //      {
    //        System.Diagnostics.Debug.WriteLine("Failed to deserialise brep");
    //        return null;
    //      }
    //    }

    //    // Extrusions
    //    public static SpeckleExtrusion ToSpeckle(this Rhino.Geometry.Extrusion extrusion)
    //    {
    //      //extrusion.PathTangent
    //      var myExtrusion = new SpeckleExtrusion(extrusion.Profile3d(0, 0).ToSpeckle(), extrusion.PathStart.DistanceTo(extrusion.PathEnd), extrusion.IsCappedAtBottom);

    //      myExtrusion.PathStart = extrusion.PathStart.ToSpeckle();
    //      myExtrusion.PathEnd = extrusion.PathEnd.ToSpeckle();
    //      myExtrusion.PathTangent = extrusion.PathTangent.ToSpeckle();
    //      myExtrusion.ProfileTransformation = extrusion.GetProfileTransformation(0.0);
    //      var Profiles = new List<SpeckleObject>();
    //      for (int i = 0; i < extrusion.ProfileCount; i++)
    //        Profiles.Add(extrusion.Profile3d(i, 0).ToSpeckle());
    //      myExtrusion.Profiles = Profiles;
    //      myExtrusion.Properties = extrusion.UserDictionary.ToSpeckle();
    //      myExtrusion.SetHashes(myExtrusion);
    //      return myExtrusion;
    //    }

    //    public static Rhino.Geometry.Extrusion ToNative(this SpeckleExtrusion extrusion)
    //    {
    //      Curve profile = null;

    //      switch (extrusion.Profile.Type)
    //      {
    //        case SpeckleObjectType.Curve:
    //          profile = ((SpeckleCurve)extrusion.Profile).ToNative();
    //          break;
    //        case SpeckleObjectType.Polycurve:
    //          profile = (((SpecklePolycurve)extrusion.Profile).ToNative());
    //          if (!profile.IsClosed)
    //            profile.Reverse();
    //          break;
    //        case SpeckleObjectType.Polyline:
    //          profile = ((SpecklePolyline)extrusion.Profile).ToNative();
    //          if (!profile.IsClosed)
    //            profile.Reverse();
    //          break;
    //        case SpeckleObjectType.Arc:
    //          profile = ((SpeckleArc)extrusion.Profile).ToNative();
    //          break;
    //        case SpeckleObjectType.Circle:
    //          profile = ((SpeckleCircle)extrusion.Profile).ToNative();
    //          break;
    //        case SpeckleObjectType.Ellipse:
    //          profile = ((SpeckleEllipse)extrusion.Profile).ToNative();
    //          break;
    //        case SpeckleObjectType.Line:
    //          profile = ((SpeckleLine)extrusion.Profile).ToNative();
    //          break;
    //        default:
    //          profile = null;
    //          break;
    //      }

    //      if (profile == null) return null;

    //      var myExtrusion = Extrusion.Create(profile.ToNurbsCurve(), (double)extrusion.Length, (bool)extrusion.Capped);

    //      myExtrusion.UserDictionary.ReplaceContentsWith(extrusion.Properties.ToNative());
    //      return myExtrusion;
    //    }

    //    // Texts & Annotations
    //    public static SpeckleAnnotation ToSpeckle(this TextEntity textentity)
    //    {
    //      Rhino.DocObjects.Font font = Rhino.RhinoDoc.ActiveDoc.Fonts[textentity.FontIndex];

    //      var myAnnotation = new SpeckleAnnotation();
    //      myAnnotation.Text = textentity.Text;
    //      myAnnotation.Plane = textentity.Plane.ToSpeckle();
    //      myAnnotation.FontName = font.FaceName;
    //      myAnnotation.TextHeight = textentity.TextHeight;
    //      myAnnotation.Bold = font.Bold;
    //      myAnnotation.Italic = font.Italic;
    //      myAnnotation.SetHashes(myAnnotation);

    //      return myAnnotation;
    //    }

    //    public static SpeckleAnnotation ToSpeckle(this TextDot textdot)
    //    {
    //      var myAnnotation = new SpeckleAnnotation();
    //      myAnnotation.Text = textdot.Text;
    //      myAnnotation.Location = textdot.Point.ToSpeckle();
    //      myAnnotation.SetHashes(myAnnotation);

    //      return myAnnotation;
    //    }

    //    public static object ToNative(this SpeckleAnnotation annot)
    //    {
    //      if (annot.Plane != null)
    //      {
    //        // TEXT ENTITIY 
    //        var textEntity = new TextEntity()
    //        {
    //          Text = annot.Text,
    //          Plane = annot.Plane.ToNative(),
    //          FontIndex = Rhino.RhinoDoc.ActiveDoc.Fonts.FindOrCreate(annot.FontName, (bool)annot.Bold, (bool)annot.Italic),
    //          TextHeight = (double)annot.TextHeight
    //        };
    //#if R6
    //                var dimStyleIndex = Rhino.RhinoDoc.ActiveDoc.DimStyles.Add("Speckle");
    //                var dimStyle = new Rhino.DocObjects.DimensionStyle
    //                {
    //                    TextHeight = (double)annot.TextHeight,
    //                    Font = new Rhino.DocObjects.Font(annot.FontName, Rhino.DocObjects.Font.FontWeight.Bold, Rhino.DocObjects.Font.FontStyle.Italic, false, false)
    //                };
    //                Rhino.RhinoDoc.ActiveDoc.DimStyles.Modify(dimStyle, dimStyleIndex, true);

    //                textEntity.DimensionStyleId = Rhino.RhinoDoc.ActiveDoc.DimStyles[dimStyleIndex].Id;

    //#endif

    //        return textEntity;
    //      }
    //      else
    //      {
    //        // TEXT DOT!
    //        var myTextdot = new TextDot(annot.Text, annot.Location.ToNative().Location);
    //        myTextdot.UserDictionary.ReplaceContentsWith(annot.Properties.ToNative());
    //        return myTextdot;
    //      }
    //    }


    // Blocks and groups
    // TODO

  }
}
