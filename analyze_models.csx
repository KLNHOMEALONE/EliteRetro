using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// Minimal types to run analysis
public struct Vertex3
{
    public float X, Y, Z;
    public Vertex3(float x, float y, float z) { X = x; Y = y; Z = z; }
}

public class Edge
{
    public int A, B;
    public Color Color;
    public Edge(int a, int b, Color c) { A = a; B = b; Color = c; }
}

public class Face
{
    public int[] Vertices;
    public Face(int[] v) { Vertices = v; }
}

public class ModelAnalysis
{
    public string Name;
    public List<Vertex3> Vertices;
    public List<Edge> Edges;
    public List<Face> Faces;

    public void Analyze()
    {
        Console.WriteLine($"\n{'='*60}");
        Console.WriteLine($"  {Name}");
        Console.WriteLine($"{'='*60}");
        Console.WriteLine($"Vertices: {Vertices.Count}, Edges: {Edges.Count}, Faces: {Faces.Count}\n");

        // Build edge set (normalized: smaller index first)
        var edgeSet = new HashSet<(int, int)>();
        foreach (var e in Edges)
        {
            var pair = e.A < e.B ? (e.A, e.B) : (e.B, e.A);
            edgeSet.Add(pair);
        }

        // Analyze each face
        var missingEdges = new List<string>();
        for (int f = 0; f < Faces.Count; f++)
        {
            var face = Faces[f];
            var v = face.Vertices;

            // Get first 3 vertices for normal computation
            var v0 = Vertices[v[0]];
            var v1 = Vertices[v[1]];
            var v2 = Vertices[v[2]];

            // Edge vectors
            var e1 = new Vector3(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
            var e2 = new Vector3(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);

            // Cross product (normal)
            var normal = Vector3.Cross(e1, e2);

            // Face centroid
            float cx = 0, cy = 0, cz = 0;
            foreach (var vi in v)
            {
                cx += Vertices[vi].X;
                cy += Vertices[vi].Y;
                cz += Vertices[vi].Z;
            }
            cx /= v.Length;
            cy /= v.Length;
            cz /= v.Length;

            var centroid = new Vector3(cx, cy, cz);
            var dot = Vector3.Dot(normal, centroid);

            string winding = dot > 0 ? "OUTWARD (OK)" : "INWARD (NEEDS REVERSE)";

            Console.WriteLine($"Face {f}: [{string.Join(", ", v)}]");
            Console.WriteLine($"  Normal: ({normal.X:F2}, {normal.Y:F2}, {normal.Z:F2})");
            Console.WriteLine($"  Centroid: ({centroid.X:F2}, {centroid.Y:F2}, {centroid.Z:F2})");
            Console.WriteLine($"  Dot(normal, centroid): {dot:F2}  -> {winding}");

            // Check boundary edges
            for (int i = 0; i < v.Length; i++)
            {
                int a = v[i];
                int b = v[(i + 1) % v.Length];
                var pair = a < b ? (a, b) : (b, a);
                if (!edgeSet.Contains(pair))
                {
                    missingEdges.Add($"  Face {f}: edge ({a},{b})");
                    Console.WriteLine($"  MISSING EDGE: ({a},{b})");
                }
            }
            Console.WriteLine();
        }

        if (missingEdges.Count > 0)
        {
            Console.WriteLine("MISSING EDGES:");
            foreach (var me in missingEdges)
                Console.WriteLine(me);
        }
        else
        {
            Console.WriteLine("All face boundary edges are present in edge list.");
        }

        // Suggest corrected faces
        Console.WriteLine("\nSuggested corrected face definitions:");
        Console.Write("new Face[]\n{\n");
        for (int f = 0; f < Faces.Count; f++)
        {
            var face = Faces[f];
            var v = face.Vertices;

            // Get first 3 vertices for normal computation
            var v0 = Vertices[v[0]];
            var v1 = Vertices[v[1]];
            var v2 = Vertices[v[2]];

            var e1 = new Vector3(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
            var e2 = new Vector3(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);
            var normal = Vector3.Cross(e1, e2);

            float cx = 0, cy = 0, cz = 0;
            foreach (var vi in v)
            {
                cx += Vertices[vi].X;
                cy += Vertices[vi].Y;
                cz += Vertices[vi].Z;
            }
            cx /= v.Length; cy /= v.Length; cz /= v.Length;
            var centroid = new Vector3(cx, cy, cz);
            var dot = Vector3.Dot(normal, centroid);

            int[] corrected;
            if (dot > 0)
                corrected = v.ToArray();
            else
                corrected = v.Reverse().ToArray();

            Console.WriteLine($"    new(new[] {{ {string.Join(", ", corrected)} }}),");
        }
        Console.WriteLine("};");

        // Complete edge list
        Console.WriteLine("\nComplete edge list (from face boundaries):");
        var allBoundaryEdges = new SortedSet<(int, int)>();
        foreach (var face in Faces)
        {
            var v = face.Vertices;
            for (int i = 0; i < v.Length; i++)
            {
                int a = v[i];
                int b = v[(i + 1) % v.Length];
                var pair = a < b ? (a, b) : (b, a);
                allBoundaryEdges.Add(pair);
            }
        }
        foreach (var e in allBoundaryEdges)
            Console.WriteLine($"    ({e.Item1}, {e.Item2}),");
    }
}

public class Program
{
    public static void Main()
    {
        // Gecko
        var gecko = new ModelAnalysis
        {
            Name = "Gecko",
            Vertices = new List<Vertex3>
            {
                new Vertex3(  0,   0,  50),   // 0 - nose
                new Vertex3(-40, -10, -20),   // 1 - port rear lower
                new Vertex3( 40, -10, -20),   // 2 - starboard rear lower
                new Vertex3(-40,  10, -20),   // 3 - port rear upper
                new Vertex3( 40,  10, -20),   // 4 - starboard rear upper
                new Vertex3(  0, -15,  10),   // 5 - lower center
                new Vertex3(  0,  15,  10),   // 6 - upper center
                new Vertex3(-20,  -8,   0),   // 7 - port lower mid
                new Vertex3( 20,  -8,   0),   // 8 - starboard lower mid
                new Vertex3(-20,   8,   0),   // 9 - port upper mid
                new Vertex3( 20,   8,   0),   // 10 - starboard upper mid
                new Vertex3(  0,   0, -20),   // 11 - rear center
            },
            Edges = new List<Edge>
            {
                new Edge(0, 1, Color.Orange),
                new Edge(0, 2, Color.Orange),
                new Edge(0, 3, Color.Orange),
                new Edge(0, 4, Color.Orange),
                new Edge(1, 2, Color.Lime),
                new Edge(3, 4, Color.Lime),
                new Edge(1, 3, Color.Lime),
                new Edge(2, 4, Color.Lime),
                new Edge(0, 5, Color.Lime),
                new Edge(0, 6, Color.Lime),
                new Edge(5, 7, Color.Lime),
                new Edge(5, 8, Color.Lime),
                new Edge(6, 9, Color.Lime),
                new Edge(6, 10, Color.Lime),
                new Edge(1, 7, Color.Lime),
                new Edge(2, 8, Color.Lime),
                new Edge(3, 9, Color.Lime),
            },
            Faces = new List<Face>
            {
                new Face(new[] { 0, 1, 3 }),
                new Face(new[] { 0, 3, 4, 2 }),
                new Face(new[] { 0, 2, 1 }),
                new Face(new[] { 0, 5, 8, 7 }),
                new Face(new[] { 0, 7, 9, 6 }),
                new Face(new[] { 0, 6, 10, 8 }),
                new Face(new[] { 1, 7, 5 }),
                new Face(new[] { 2, 8, 5 }),
                new Face(new[] { 3, 4, 11 }),
            }
        };
        gecko.Analyze();

        // Krait
        var krait = new ModelAnalysis
        {
            Name = "Krait",
            Vertices = new List<Vertex3>
            {
                new Vertex3(  0, -18,   0),   // 0 - lower nose
                new Vertex3(  0,  -9, -45),   // 1 - lower mid
                new Vertex3( 43,   0, -45),   // 2 - starboard mid
                new Vertex3( 69,  -3,   0),   // 3 - starboard nose
                new Vertex3( 43, -14,  28),   // 4 - starboard front
                new Vertex3(-43,   0, -45),   // 5 - port mid
                new Vertex3(-69,  -3,   0),   // 6 - port nose
                new Vertex3(-43, -14,  28),   // 7 - port front
                new Vertex3( 26,  -7,  73),   // 8 - starboard wing
                new Vertex3(-26,  -7,  73),   // 9 - port wing
                new Vertex3( 43,  14,  28),   // 10 - starboard upper front
                new Vertex3(-43,  14,  28),   // 11 - port upper front
                new Vertex3(  0,   9, -45),   // 12 - upper mid
                new Vertex3(-17,   0, -45),   // 13 - port upper mid detail
                new Vertex3( 17,   0, -45),   // 14 - starboard upper mid detail
                new Vertex3(  0,  -4, -45),   // 15 - center detail
                new Vertex3(  0,   4, -45),   // 16 - center upper detail
                new Vertex3(  0,  -7,  73),   // 17 - rear lower
                new Vertex3(  0,  -7,  83),   // 18 - rear lower extended
            },
            Edges = new List<Edge>
            {
                new Edge(0, 1, Color.Orange),
                new Edge(0, 4, Color.Orange),
                new Edge(0, 7, Color.Orange),
                new Edge(1, 2, Color.Lime),
                new Edge(2, 3, Color.Lime),
                new Edge(3, 8, Color.Lime),
                new Edge(8, 9, Color.Lime),
                new Edge(6, 9, Color.Lime),
                new Edge(5, 6, Color.Lime),
                new Edge(1, 5, Color.Lime),
                new Edge(3, 4, Color.Lime),
                new Edge(4, 8, Color.Lime),
                new Edge(6, 7, Color.Lime),
                new Edge(7, 9, Color.Lime),
                new Edge(2, 12, Color.Lime),
                new Edge(5, 12, Color.Lime),
                new Edge(10, 12, Color.Lime),
                new Edge(11, 12, Color.Lime),
                new Edge(10, 11, Color.Lime),
                new Edge(6, 11, Color.Lime),
                new Edge(9, 11, Color.Lime),
            },
            Faces = new List<Face>
            {
                new Face(new[] { 0, 1, 2, 4 }),
                new Face(new[] { 0, 4, 7 }),
                new Face(new[] { 0, 7, 1 }),
                new Face(new[] { 1, 5, 12 }),
                new Face(new[] { 1, 12, 2 }),
                new Face(new[] { 0, 1, 5, 7 }),
            }
        };
        krait.Analyze();

        // Worm
        var worm = new ModelAnalysis
        {
            Name = "Worm",
            Vertices = new List<Vertex3>
            {
                new Vertex3(  0,   0,  35),   // 0 - nose
                new Vertex3(-26, -10, -15),   // 1 - port rear lower
                new Vertex3( 26, -10, -15),   // 2 - starboard rear lower
                new Vertex3(-26,  10, -15),   // 3 - port rear upper
                new Vertex3( 26,  10, -15),   // 4 - starboard rear upper
                new Vertex3(  0, -14,   5),   // 5 - lower center
                new Vertex3(  0,  14,   5),   // 6 - upper center
                new Vertex3(-15,  -7,  10),   // 7 - port wing lower
                new Vertex3( 15,  -7,  10),   // 8 - starboard wing lower
                new Vertex3(  0,   0, -15),   // 9 - rear center
            },
            Edges = new List<Edge>
            {
                new Edge(0, 1, Color.Orange),
                new Edge(0, 2, Color.Orange),
                new Edge(0, 3, Color.Orange),
                new Edge(0, 4, Color.Orange),
                new Edge(1, 2, Color.Lime),
                new Edge(3, 4, Color.Lime),
                new Edge(1, 3, Color.Lime),
                new Edge(2, 4, Color.Lime),
                new Edge(0, 5, Color.Lime),
                new Edge(0, 6, Color.Lime),
                new Edge(5, 7, Color.Lime),
                new Edge(5, 8, Color.Lime),
                new Edge(1, 7, Color.Lime),
                new Edge(2, 8, Color.Lime),
                new Edge(3, 6, Color.Lime),
                new Edge(4, 6, Color.Lime),
            },
            Faces = new List<Face>
            {
                new Face(new[] { 0, 1, 3 }),
                new Face(new[] { 0, 3, 4, 2 }),
                new Face(new[] { 0, 2, 1 }),
                new Face(new[] { 0, 5, 8, 7 }),
                new Face(new[] { 0, 7, 6 }),
                new Face(new[] { 0, 6, 8 }),
                new Face(new[] { 1, 7, 5 }),
                new Face(new[] { 2, 8, 5 }),
            }
        };
        worm.Analyze();
    }
}
