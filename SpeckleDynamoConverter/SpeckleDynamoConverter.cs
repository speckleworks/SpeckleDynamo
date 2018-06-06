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


    #region Helper Methods

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

    public static bool Threshold(double value1, double value2, double error = EPS)
    {
      return Math.Abs(value1 - value2) <= error;
    }

    public static double Median(double min, double max)
    {
      return ((max - min) * 0.5) + min;
    }

    #endregion


    #region Numbers
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
    #endregion


    #region Booleans
    public static SpeckleBoolean ToSpeckle(this bool b)
    {
      return new SpeckleBoolean(b);
    }

    public static bool? ToNative(this SpeckleBoolean b)
    {
      return b.Value;
    }
    #endregion
<<<<<<< HEAD


=======


>>>>>>> origin/dev
    #region Strings
    public static SpeckleString ToSpeckle(this string b)
    {
      return new SpeckleString(b);
    }

    public static string ToNative(this SpeckleString b)
    {
      return b.Value;
    }
    #endregion



    #region Points

    /// <summary>
    /// DS Point to SpecklePoint
    /// </summary>
    /// <param name="pt"></param>
    /// <returns></returns>
    public static SpecklePoint ToSpeckle(this Point pt)
    {
      return new SpecklePoint(pt.X, pt.Y, pt.Z);
    }
<<<<<<< HEAD

    /// <summary>
    /// Speckle Point to DS Point
    /// </summary>
    /// <param name="pt"></param>
    /// <returns></returns>
    public static Point ToNative(this SpecklePoint pt)
    {
      var myPoint = Point.ByCoordinates(pt.Value[0], pt.Value[1], pt.Value[2]);

      return myPoint;
    }

=======

    /// <summary>
    /// Speckle Point to DS Point
    /// </summary>
    /// <param name="pt"></param>
    /// <returns></returns>
    public static Point ToNative(this SpecklePoint pt)
    {
      var myPoint = Point.ByCoordinates(pt.Value[0], pt.Value[1], pt.Value[2]);

      return myPoint;
    }

>>>>>>> origin/dev
    /// <summary>
    /// Array of point coordinates to array of DS Points
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static Point[] ToPoints(this IEnumerable<double> arr)
    {
      if (arr.Count() % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      Point[] points = new Point[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
        points[k++] = Point.ByCoordinates(asArray[i - 2], asArray[i - 1], asArray[i]);

      return points;
    }

    /// <summary>
    /// Array of DS Points to array of point coordinates
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static double[] ToFlatArray(this IEnumerable<Point> points)
    {
      return points.SelectMany(pt => pt.ToArray()).ToArray();
    }

    #endregion

    #region Vectors
    /// <summary>
    /// DS Vector to SpeckleVector
    /// </summary>
    /// <param name="vc"></param>
    /// <returns></returns>
    public static SpeckleVector ToSpeckle(this Vector vc)
    {
      return new SpeckleVector(vc.X, vc.Y, vc.Z);
    }

    /// <summary>
    /// SpeckleVector to DS Vector
    /// </summary>
    /// <param name="vc"></param>
    /// <returns></returns>
    public static Vector ToNative(this SpeckleVector vc)
    {
      return Vector.ByCoordinates(vc.Value[0], vc.Value[1], vc.Value[2]);
    }

    /// <summary>
    /// DS Vector to array of coordinates
    /// </summary>
    /// <param name="vc"></param>
    /// <returns></returns>
    public static double[] ToArray(this Vector vc)
    {
      return new double[] { vc.X, vc.Y, vc.Z };
    }

    /// <summary>
    /// Array of coordinates to DS Vector
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static Vector ToVector(this double[] arr)
    {
      return Vector.ByCoordinates(arr[0], arr[1], arr[2]);
    }
    #endregion


    #region Planes
    /// <summary>
    /// DS Plane to SpecklePlane
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    public static SpecklePlane ToSpeckle(this Plane plane)
    {
      return new SpecklePlane(plane.Origin.ToSpeckle(), plane.Normal.ToSpeckle(), plane.XAxis.ToSpeckle(), plane.YAxis.ToSpeckle());
    }

    /// <summary>
    /// SpecklePlane to DS Plane
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    public static Plane ToNative(this SpecklePlane plane)
    {
      var returnPlane = Plane.ByOriginNormal(plane.Origin.ToNative(), plane.Normal.ToNative());
      return returnPlane;
    }
    #endregion
<<<<<<< HEAD


=======


>>>>>>> origin/dev
    #region Linear

    /// <summary>
    /// DS Line to SpeckleLine
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static SpeckleLine ToSpeckle(this Line line)
    {
      return new SpeckleLine((new Point[] { line.StartPoint, line.EndPoint }).ToFlatArray());
    }

    /// <summary>
    /// SpeckleLine to DS Line
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Line ToNative(this SpeckleLine line)
    {
      var pts = line.Value.ToPoints();
      return Line.ByStartPointEndPoint(pts[0], pts[1]);
    }

    /// <summary>
    /// DS Polygon to closed SpecklePolyline
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
<<<<<<< HEAD

    /// <summary>
    /// DS Rectangle to SpecklePolyline
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static SpecklePolyline ToSpeckle(this Rectangle rect)
    {
      return (rect as Polygon).ToSpeckle();
    }

    /// <summary>
    /// SpecklePolyline to DS Rectangle if closed , four points and sides parallel; 
    /// DS Polygon if closed or DS Polycurve otherwise
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

        if (points.Count() == 4 && Threshold(dot, 0))
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

    #endregion

    #region Curves Helper Methods

=======

    /// <summary>
    /// DS Rectangle to SpecklePolyline
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static SpecklePolyline ToSpeckle(this Rectangle rect)
    {
      return (rect as Polygon).ToSpeckle();
    }

    /// <summary>
    /// SpecklePolyline to DS Rectangle if closed , four points and sides parallel; 
    /// DS Polygon if closed or DS Polycurve otherwise
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

        if (points.Count() == 4 && Threshold(dot, 0))
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

    #endregion

    #region Curves Helper Methods
>>>>>>> origin/dev

    public static bool IsLinear(this Curve curve)
    {
      if (curve.IsClosed) { return false; }
      //Dynamo cannot be trusted when less than 1e-6
      var extremesDistance = curve.StartPoint.DistanceTo(curve.EndPoint);
      return Threshold(curve.Length, extremesDistance);
    }

    public static Line GetAsLine(this Curve curve)
    {
      if (curve.IsClosed) { throw new ArgumentException("Curve is closed, cannot be a Line"); }
      return Line.ByStartPointEndPoint(curve.StartPoint, curve.EndPoint);
    }

    public static bool IsPolyline(this PolyCurve polycurve)
    {
      return polycurve.Curves().All(c => c.IsLinear());
    }

    public static bool IsArc(this Curve curve)
    {
      if (curve.IsClosed) { return false; }
      using (Point midPoint = curve.PointAtParameter(0.5))
      using (Arc arc = Arc.ByThreePoints(curve.StartPoint, midPoint, curve.EndPoint))
      {
        return Threshold(arc.Length, curve.Length);
      }
<<<<<<< HEAD
    }

    public static Arc GetAsArc(this Curve curve)
    {
      if (curve.IsClosed) { throw new ArgumentException("Curve is closed, cannot be an Arc"); }
      Point midPoint = curve.PointAtParameter(0.5);
      return Arc.ByThreePoints(curve.StartPoint, midPoint, curve.EndPoint);
    } 

    public static bool IsCircle(this Curve curve)
    {
      if (!curve.IsClosed) { return false; }
      using(Point midPoint = curve.PointAtParameter(0.5))
      {
        double radius = curve.StartPoint.DistanceTo(midPoint) * 0.5;
        return Threshold(radius, (curve.Length) / (2 * Math.PI));
      }
    }

    public static Circle GetAsCircle(this Curve curve)
    {
      if (!curve.IsClosed) { throw new ArgumentException("Curve is not closed, cannot be a Circle"); }

      Point start = curve.StartPoint;
      using (Point midPoint = curve.PointAtParameter(0.5))
      {
        Point centre = Point.ByCoordinates(
          Median(start.X, midPoint.X),
          Median(start.Y, midPoint.Y),
          Median(start.Z, midPoint.Z)
        );

        return Circle.ByCenterPointRadiusNormal(
            centre,
            centre.DistanceTo(start),
            curve.Normal
        );
      }
    }

    public static bool IsEllipse(this Curve curve)
    {
      if (!curve.IsClosed) { return false; }

      //http://www.numericana.com/answer/ellipse.htm
      double[] parameters = new double[4] { 0, 0.25, 0.5, 0.75 };
      Point[] points = parameters.Select(p => curve.PointAtParameter(p)).ToArray();
      double a = points[0].DistanceTo(points[2]) * 0.5; // Max Radius
      double b = points[1].DistanceTo(points[3]) * 0.5; // Min Radius
      points.ForEach(p => p.Dispose());

      double h = Math.Pow(a - b, 2) / Math.Pow(a + b, 2);
      double perimeter = Math.PI * (a + b) * (1 + (3 * h / (10 + Math.Sqrt(4 - 3 * h))));

      return Threshold(curve.Length, perimeter, 1e-5); //Ellipse perimeter is an approximation
    }

    public static Ellipse GetAsEllipse(this Curve curve)
    {
      if (!curve.IsClosed) { throw new ArgumentException("Curve is not closed, cannot be an Ellipse"); }
=======
    }

    public static Arc GetAsArc(this Curve curve)
    {
      if (curve.IsClosed) { throw new ArgumentException("Curve is closed, cannot be an Arc"); }
      using (Point midPoint = curve.PointAtParameter(0.5))
      {
        return Arc.ByThreePoints(curve.StartPoint, midPoint, curve.EndPoint);
      }
      
    } 

    public static bool IsCircle(this Curve curve)
    {
      if (!curve.IsClosed) { return false; }
      using(Point midPoint = curve.PointAtParameter(0.5))
      {
        double radius = curve.StartPoint.DistanceTo(midPoint) * 0.5;
        return Threshold(radius, (curve.Length) / (2 * Math.PI));
      }
    }

    public static Circle GetAsCircle(this Curve curve)
    {
      if (!curve.IsClosed) { throw new ArgumentException("Curve is not closed, cannot be a Circle"); }

      Point start = curve.StartPoint;
      using (Point midPoint = curve.PointAtParameter(0.5))
      using (Point centre = Point.ByCoordinates(Median(start.X, midPoint.X), Median(start.Y, midPoint.Y), Median(start.Z, midPoint.Z) ))
      {
        return Circle.ByCenterPointRadiusNormal(
            centre,
            centre.DistanceTo(start),
            curve.Normal
        );
      }
    }

    public static bool IsEllipse(this Curve curve)
    {
      if (!curve.IsClosed) { return false; }

      //http://www.numericana.com/answer/ellipse.htm
>>>>>>> origin/dev
      double[] parameters = new double[4] { 0, 0.25, 0.5, 0.75 };
      Point[] points = parameters.Select(p => curve.PointAtParameter(p)).ToArray();
      double a = points[0].DistanceTo(points[2]) * 0.5; // Max Radius
      double b = points[1].DistanceTo(points[3]) * 0.5; // Min Radius
<<<<<<< HEAD

      Point centre = Point.ByCoordinates(
        Median(points[0].X, points[2].X),
        Median(points[0].Y, points[2].Y),
        Median(points[0].Z, points[2].Z)
        );

      points.ForEach(p => p.Dispose());

      return Ellipse.ByPlaneRadii(
          Plane.ByOriginNormalXAxis(centre, curve.Normal, Vector.ByTwoPoints(centre, curve.StartPoint)),
          a,
          b
          );
    }

=======
      points.ForEach(p => p.Dispose());

      double h = Math.Pow(a - b, 2) / Math.Pow(a + b, 2);
      double perimeter = Math.PI * (a + b) * (1 + (3 * h / (10 + Math.Sqrt(4 - 3 * h))));

      return Threshold(curve.Length, perimeter, 1e-5); //Ellipse perimeter is an approximation
    }

    public static Ellipse GetAsEllipse(this Curve curve)
    {
      if (!curve.IsClosed) { throw new ArgumentException("Curve is not closed, cannot be an Ellipse"); }
      double[] parameters = new double[4] { 0, 0.25, 0.5, 0.75 };
      Point[] points = parameters.Select(p => curve.PointAtParameter(p)).ToArray();
      double a = points[0].DistanceTo(points[2]) * 0.5; // Max Radius
      double b = points[1].DistanceTo(points[3]) * 0.5; // Min Radius

      using (Point centre = Point.ByCoordinates( Median(points[0].X, points[2].X), Median(points[0].Y, points[2].Y), Median(points[0].Z, points[2].Z) ))
      {
        points.ForEach(p => p.Dispose());

        return Ellipse.ByPlaneRadii(
            Plane.ByOriginNormalXAxis(centre, curve.Normal, Vector.ByTwoPoints(centre, curve.StartPoint)),
            a,
            b
            );
      }
    }

>>>>>>> origin/dev
    #endregion

    #region Curves

<<<<<<< HEAD

=======
>>>>>>> origin/dev
    /// <summary>
    /// DS Circle to SpeckleCircle
    /// </summary>
    /// <param name="circ"></param>
    /// <returns></returns>
    public static SpeckleCircle ToSpeckle(this Circle circ)
    {
      return new SpeckleCircle(
        circ.CenterPoint.ToSpeckle(),
        circ.Normal.ToSpeckle(),
        circ.Radius
        );
    }

    /// <summary>
    /// SpeckleCircle to DS Circle
    /// </summary>
    /// <param name="circ"></param>
    /// <returns></returns>
    public static Circle ToNative(this SpeckleCircle circ)
    {
      return Circle.ByCenterPointRadiusNormal(
        circ.Center.ToNative(),
        circ.Radius.Value,
        circ.Normal.ToNative()
        );
    }


    /// <summary>
    /// DS Arc to SpeckleArc
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
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

    /// <summary>
    /// SpeckleArc to DS Arc
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
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


    /// <summary>
    /// DS Ellipse to SpeckleEllipse
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static SpeckleEllipse ToSpeckle(this Ellipse e)
    {
      return new SpeckleEllipse(
          Plane.ByOriginNormal(e.CenterPoint, e.Normal).ToSpeckle(),
          e.MajorAxis.Length,
          e.MinorAxis.Length
      );
    }

    /// <summary>
    /// SpeckleEllipseto DS Ellipse
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static Ellipse ToNative(this SpeckleEllipse e)
    {
      return Ellipse.ByPlaneRadii(
          e.Plane.ToNative(),
          e.FirstRadius.Value,
          e.SecondRadius.Value
      );
    }

    /// <summary>
    /// DS EllipsArc to SpeckleCurve?????
    /// </summary>
    /// <param name="arc"></param>
    /// <returns></returns>
<<<<<<< HEAD
    public static SpeckleCurve ToSpeckle(this EllipseArc arc)
    {
      //TODO: Implement EllipseArc converter
      throw new NotImplementedException("EllipsArc not implemented yet.");
    }

    public static EllipseArc ToNative(this SpeckleCurve arc)
    {
      //TODO: Implement EllipseArc converter
      throw new NotImplementedException("EllipsArc not implemented yet.");
    }
=======
    public static SpeckleObject ToSpeckle(this EllipseArc arc)
    {
      //EllipseArcs as NurbsCurves
      using (NurbsCurve nurbsCurve = arc.ToNurbsCurve())
      {
        return nurbsCurve.ToSpeckle();
      }
    }

    //public static EllipseArc ToNative(this SpeckleCurve arc)
    //{
    //  //TODO: Implement EllipseArc converter
    //  throw new NotImplementedException("EllipsArc not implemented yet.");
    //}
>>>>>>> origin/dev

    /// <summary>
    /// DS Polycurve to SpecklePolyline if all curves are linear
    /// SpecklePolycurve otherwise
    /// </summary>
    /// <param name="polycurve"></param>
    /// <returns name="speckleObject"></returns>
    public static SpeckleObject ToSpeckle(this PolyCurve polycurve)
    {
      if (polycurve.IsPolyline())
      {
<<<<<<< HEAD
        var points = polycurve.Curves().Select(c => c.StartPoint).ToList();
        points.Add(polycurve.Curves().Last().EndPoint);
        return new SpecklePolyline(points.ToFlatArray());
=======
        var points = polycurve.Curves().SelectMany(c => c.StartPoint.ToArray()).ToList();
        points.AddRange(polycurve.Curves().Last().EndPoint.ToArray());
        return new SpecklePolyline(points);
>>>>>>> origin/dev
      }
      else
      {
        SpecklePolycurve spkPolycurve = new SpecklePolycurve();
        spkPolycurve.Segments = polycurve.Curves().Select(c => c.ToSpeckle()).ToList();
        spkPolycurve.GenerateHash();
        return spkPolycurve;
      }
    }

<<<<<<< HEAD
=======
    public static PolyCurve ToNative (this SpecklePolycurve polycurve)
    {
      Curve[] curves = new Curve[polycurve.Segments.Count];
      for(var i = 0; i < polycurve.Segments.Count; i++)
      {
        switch (polycurve.Segments[i])
        {
          case SpeckleLine curve:
            curves[i] = curve.ToNative();
            break;
          case SpeckleArc curve:
            curves[i] = curve.ToNative();
            break;
          case SpeckleCircle curve:
            curves[i] = curve.ToNative();
            break;
          case SpeckleEllipse curve:
            curves[i] = curve.ToNative();
            break;
          case SpecklePolycurve curve:
            curves[i] = curve.ToNative();
            break;
          case SpecklePolyline curve:
            curves[i] = curve.ToNative();
            break;
          case SpeckleCurve curve:
            curves[i] = curve.ToNative();
            break;
        }
      }

      return PolyCurve.ByJoinedCurves(curves);
    }
>>>>>>> origin/dev

    public static SpeckleObject ToSpeckle(this Curve curve)
    {
      if (curve.IsLinear())
      {
        using (Line line = curve.GetAsLine()) { return line.ToSpeckle(); }
      }
      if (curve.IsArc())
      {
        using (Arc arc = curve.GetAsArc()) { return arc.ToSpeckle(); }
      }
      if (curve.IsCircle())
      {
        using (Circle circle = curve.GetAsCircle()) { return circle.ToSpeckle(); }
      }
      if (curve.IsEllipse())
      {
        using (Ellipse ellipse = curve.GetAsEllipse()) { return ellipse.ToSpeckle(); }
      }

<<<<<<< HEAD
      throw new NotImplementedException("Not implemented yet, I'm gonna sleep.");

      // NurbsCurve,shit.
      using (PolyCurve polycurve = PolyCurve.ByJoinedCurves(curve.ApproximateWithArcAndLineSegments()))
      {
        SpecklePolyline displaValue;
        if (polycurve.NumberOfCurves == 1)
        {
          displaValue = new SpecklePolyline(
            new Point[2] { polycurve.Curves().First().StartPoint, polycurve.Curves().First().EndPoint }.ToFlatArray()
            );
        }
        else
        {
          displaValue = polycurve.ToSpeckle() as SpecklePolyline;
        }
      }

=======
      // Convert as NurbsCurve
      return curve.ToNurbsCurve().ToSpeckle();

    }

    public static NurbsCurve ToNative(this SpeckleCurve curve)
    {
      var points = curve.Points.ToPoints();
      var dsKnots = curve.Knots;
      dsKnots.Insert(0, dsKnots.First());
      dsKnots.Add(dsKnots.Last());

      NurbsCurve nurbsCurve = NurbsCurve.ByControlPointsWeightsKnots(
          points,
          curve.Weights.ToArray(),
          dsKnots.ToArray(),
          curve.Degree
          );
      
      return nurbsCurve;
>>>>>>> origin/dev
    }

    public static SpeckleObject ToSpeckle(this NurbsCurve curve)
    {
      if (curve.IsLinear())
      {
        using (Line line = curve.GetAsLine()) { return line.ToSpeckle(); }
      }
      if (curve.IsArc())
      {
        using (Arc arc = curve.GetAsArc()) { return arc.ToSpeckle(); }
      }
      if (curve.IsCircle())
      {
        using (Circle circle = curve.GetAsCircle()) { return circle.ToSpeckle(); }
      }
      if (curve.IsEllipse())
      {
        using (Ellipse ellipse = curve.GetAsEllipse()) { return ellipse.ToSpeckle(); }
      }

<<<<<<< HEAD
      throw new NotImplementedException("Not implemented yet, I'm gonna sleep.");
    }

    #endregion




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
=======
      // SpeckleCurve DisplayValue
      Curve[] curves = curve.ApproximateWithArcAndLineSegments();
      List<double> polylineCoordinates = curves.SelectMany(c => new Point[2] { c.StartPoint, c.EndPoint }.ToFlatArray()).ToList();
      polylineCoordinates.AddRange(curves.Last().EndPoint.ToArray());
      curves.ForEach(c => c.Dispose());

      SpecklePolyline displaValue = new SpecklePolyline(polylineCoordinates);
      List<double> dsKnots = curve.Knots().ToList(); 
      dsKnots.RemoveAt(dsKnots.Count - 1);
      dsKnots.RemoveAt(0);

      SpeckleCurve spkCurve = new SpeckleCurve(displaValue);
      spkCurve.Weights = curve.Weights().ToList();
      spkCurve.Points = curve.ControlPoints().ToFlatArray().ToList();
      spkCurve.Knots = dsKnots;
      spkCurve.Degree = curve.Degree;
      spkCurve.Periodic = curve.IsPeriodic;
      spkCurve.Rational = curve.IsRational;
      spkCurve.Closed = curve.IsClosed;
      //spkCurve.Domain
      //spkCurve.Properties

      spkCurve.GenerateHash();
      return spkCurve;

    }

    public static SpeckleObject ToSpeckle(this Helix helix)
    {
      using(NurbsCurve nurbsCurve = helix.ToNurbsCurve())
      {
        return nurbsCurve.ToSpeckle();
      }
    }

    #endregion
>>>>>>> origin/dev

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
<<<<<<< HEAD

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
=======
>>>>>>> origin/dev

  }
}
