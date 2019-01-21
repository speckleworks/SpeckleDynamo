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
using Autodesk.Revit.DB;
using Revit.Elements;
using RevitServices.Persistence;
using Solid = Autodesk.Revit.DB.Solid;
using Face = Autodesk.Revit.DB.Face;
using Mesh = Autodesk.Revit.DB.Mesh;
using System.Diagnostics;

namespace SpeckleDynamo.Functions
{
  public static class RevitUtils
  {
    /// <summary>
    /// If true, individual curved surface facets are
    /// retained, otherwise (default) smoothing is 
    /// applied.
    /// </summary>
    static private bool RetainCurvedSurfaceFacets = false;

    // Unit conversion factors.

    const double _mm_per_inch = 25.4;
    const double _inch_per_foot = 12;
    const double _foot_to_mm = _inch_per_foot * _mm_per_inch;


    [NodeName("ByElement")]
    [NodeCategory("Speckle.Revit")]
    [NodeDescription("Creates a Dynamo mesh from an element")]
    public static Autodesk.DesignScript.Geometry.Mesh ByElement(Revit.Elements.Element element)
    {
      var e = element.InternalElement;

      BoundingBoxXYZ bb = e.get_BoundingBox(null);
      XYZ pmin = bb.Min;
      XYZ pmax = bb.Max;
      XYZ vsize = pmax - pmin;
      XYZ pmid = pmin + 0.5 * vsize;

      Options opt = new Options();
      GeometryElement geo = e.get_Geometry(opt);

      List<int> faceIndices = new List<int>();
      List<int> faceVertices = new List<int>();
      List<double> faceNormals = new List<double>();
      int[] triangleIndices = new int[3];
      XYZ[] triangleCorners = new XYZ[3];

      List<Autodesk.DesignScript.Geometry.Point> points = new List<Autodesk.DesignScript.Geometry.Point>();
      List<Autodesk.DesignScript.Geometry.IndexGroup> indices = new List<Autodesk.DesignScript.Geometry.IndexGroup>();

      // Scale the vertices to a [-1,1] cube 
      // centered around the origin. Translation
      // to the origin was already performed above.

      double scale = 2.0 / FootToMm(MaxCoord(vsize));

      foreach (GeometryObject obj in geo)
      {
        Solid solid = obj as Solid;

        if (solid != null && 0 < solid.Faces.Size)
        {
          faceIndices.Clear();
          faceVertices.Clear();
          faceNormals.Clear();

          foreach (Face face in solid.Faces)
          {
            Mesh mesh = face.Triangulate();

            int nTriangles = mesh.NumTriangles;

            IList<XYZ> vertices = mesh.Vertices;

            int nVertices = vertices.Count;

            List<int> vertexCoordsMm = new List<int>(3 * nVertices);

            // A vertex may be reused several times with 
            // different normals for different faces, so 
            // we cannot precalculate normals per vertex.
            //List<double> normals = new List<double>( 3 * nVertices );

            foreach (XYZ v in vertices)
            {
              // Translate the entire element geometry
              // to the bounding box midpoint and scale 
              // to metric millimetres.

              XYZ p = v - pmid;

              vertexCoordsMm.Add(FootToMm(p.X));
              vertexCoordsMm.Add(FootToMm(p.Y));
              vertexCoordsMm.Add(FootToMm(p.Z));
            }

            for (int i = 0; i < nTriangles; ++i)
            {
              MeshTriangle triangle = mesh.get_Triangle(i);

              for (int j = 0; j < 3; ++j)
              {
                int k = (int)triangle.get_Index(j);
                triangleIndices[j] = k;
                triangleCorners[j] = vertices[k];
              }

              // Calculate constant triangle facet normal.

              XYZ v = triangleCorners[1]
                - triangleCorners[0];
              XYZ w = triangleCorners[2]
                - triangleCorners[0];
              XYZ triangleNormal = v
                .CrossProduct(w)
                .Normalize();

              indices.Add(IndexGroup.ByIndices((uint)triangleIndices[0], (uint)triangleIndices[1], (uint)triangleIndices[2]));

              for (int j = 0; j < 3; ++j)
              {
                int nFaceVertices = faceVertices.Count;

                Debug.Assert(nFaceVertices.Equals(faceNormals.Count),
                  "expected equal number of face vertex and normal coordinates");

                faceIndices.Add(nFaceVertices / 3);

                int i3 = triangleIndices[j] * 3;

                // Rotate the X, Y and Z directions, 
                // since the Z direction points upward 
                // in Revit as opposed to sideways or
                // outwards or forwards in WebGL.

                faceVertices.Add(vertexCoordsMm[i3 + 1]);
                faceVertices.Add(vertexCoordsMm[i3 + 2]);
                faceVertices.Add(vertexCoordsMm[i3]);

                points.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(vertexCoordsMm[i3], vertexCoordsMm[i3 + 1], vertexCoordsMm[i3 + 2]));

                if (RetainCurvedSurfaceFacets)
                {
                  faceNormals.Add(triangleNormal.Y);
                  faceNormals.Add(triangleNormal.Z);
                  faceNormals.Add(triangleNormal.X);
                }
                else
                {
                  Autodesk.Revit.DB.UV uv = face.Project(
                    triangleCorners[j]).UVPoint;

                  XYZ normal = face.ComputeNormal(uv);

                  faceNormals.Add(normal.Y);
                  faceNormals.Add(normal.Z);
                  faceNormals.Add(normal.X);
                }
              }
            }
          }



          Debug.Print("position: [{0}],",
            string.Join(", ",
              faceVertices.ConvertAll<string>(
                i => (i * scale).ToString("0.##"))));

          Debug.Print("normal: [{0}],",
            string.Join(", ",
              faceNormals.ConvertAll<string>(
                f => f.ToString("0.##"))));

          Debug.Print("indices: [{0}],",
            string.Join(", ",
              faceIndices.ConvertAll<string>(
                i => i.ToString())));
        }
      }

      return Autodesk.DesignScript.Geometry.Mesh.ByPointsFaceIndices(points, indices);
    }

    private static int FootToMm(double a)
    {
      double one_half = a > 0 ? 0.5 : -0.5;
      return (int)(a * _foot_to_mm + one_half);
    }

    private static double MaxCoord(XYZ a)
    {
      double d = Math.Abs(a.X);
      d = Math.Max(d, Math.Abs(a.Y));
      d = Math.Max(d, Math.Abs(a.Z));
      return d;
    }

  }
}
