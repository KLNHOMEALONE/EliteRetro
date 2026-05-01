using System;
using System.Collections.Generic;
using System.Linq;

public struct Vertex3
{
    public float X, Y, Z;
    public Vertex3(float x, float y, float z) { X = x; Y = y; Z = z; }
}

public class Face
{
    public int[] Vertices;
    public Face(int[] v) { Vertices = v; }
}

public class Edge
{
    public int A, B;
    public Edge(int a, int b) { A = a; B = b; }
}

class ModelAnalysis
{
    public string Name = "";
    public List<Vertex3> Vertices = new();
    public List<Edge> Edges = new();
    public List<Face> Faces = new();

    public void Analyze()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine($"  {Name}");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Vertices: {Vertices.Count}, Edges: {Edges.Count}, Faces: {Faces.Count}\n");

        var edgeSet = new HashSet<(int, int)>();
        foreach (var e in Edges)
        {
            var pair = e.A < e.B ? (e.A, e.B) : (e.B, e.A);
            edgeSet.Add(pair);
        }

        var missingEdges = new List<string>();
        var correctedFaces = new List<int[]>();

        for (int f = 0; f < Faces.Count; f++)
        {
            var face = Faces[f];
            var v = face.Vertices;

            var v0 = Vertices[v[0]];
            var v1 = Vertices[v[1]];
            var v2 = Vertices[v[2]];

            float nx = (v1.Y - v0.Y) * (v2.Z - v0.Z) - (v1.Z - v0.Z) * (v2.Y - v0.Y);
            float ny = (v1.Z - v0.Z) * (v2.X - v0.X) - (v1.X - v0.X) * (v2.Z - v0.Z);
            float nz = (v1.X - v0.X) * (v2.Y - v0.Y) - (v1.Y - v0.Y) * (v2.X - v0.X);

            float cx = 0, cy = 0, cz = 0;
            foreach (var vi in v)
            {
                cx += Vertices[vi].X;
                cy += Vertices[vi].Y;
                cz += Vertices[vi].Z;
            }
            cx /= v.Length; cy /= v.Length; cz /= v.Length;

            float dot = nx * cx + ny * cy + nz * cz;
            string winding = dot > 0 ? "OUTWARD (OK)" : "INWARD (NEEDS REVERSE)";

            Console.WriteLine($"Face {f}: [{string.Join(", ", v)}]");
            Console.WriteLine($"  Normal: ({nx:F2}, {ny:F2}, {nz:F2})");
            Console.WriteLine($"  Centroid: ({cx:F2}, {cy:F2}, {cz:F2})");
            Console.WriteLine($"  Dot(normal, centroid): {dot:F2}  => {winding}");

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

            if (dot > 0)
                correctedFaces.Add(v.ToArray());
            else
                correctedFaces.Add(v.Reverse().ToArray());
        }

        if (missingEdges.Count > 0)
        {
            Console.WriteLine("MISSING EDGES:");
            foreach (var me in missingEdges) Console.WriteLine(me);
        }
        else
        {
            Console.WriteLine("All face boundary edges are present in edge list.");
        }

        Console.WriteLine("\nCorrected face definitions:");
        Console.Write("new Face[]\n{\n");
        foreach (var cf in correctedFaces)
            Console.WriteLine($"    new(new[] {{ {string.Join(", ", cf)} }}),");
        Console.WriteLine("};");

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

        Console.WriteLine($"\nCurrent edges: {Edges.Count}, Required edges: {allBoundaryEdges.Count}");
    }
}

class Program
{
    static void Main(string[] args)
    {
        var gecko = new ModelAnalysis
        {
            Name = "Gecko",
            Vertices = new List<Vertex3>
            {
                new Vertex3(  0,   0,  50), new Vertex3(-40, -10, -20),
                new Vertex3( 40, -10, -20), new Vertex3(-40,  10, -20),
                new Vertex3( 40,  10, -20), new Vertex3(  0, -15,  10),
                new Vertex3(  0,  15,  10), new Vertex3(-20,  -8,   0),
                new Vertex3( 20,  -8,   0), new Vertex3(-20,   8,   0),
                new Vertex3( 20,   8,   0), new Vertex3(  0,   0, -20),
            },
            Edges = new List<Edge>
            {
                new Edge(0, 1), new Edge(0, 2), new Edge(0, 3), new Edge(0, 4),
                new Edge(1, 2), new Edge(3, 4), new Edge(1, 3), new Edge(2, 4),
                new Edge(0, 5), new Edge(0, 6), new Edge(5, 7), new Edge(5, 8),
                new Edge(6, 9), new Edge(6, 10), new Edge(1, 7), new Edge(2, 8),
                new Edge(3, 9),
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

        var krait = new ModelAnalysis
        {
            Name = "Krait",
            Vertices = new List<Vertex3>
            {
                new Vertex3(  0, -18,   0), new Vertex3(  0,  -9, -45),
                new Vertex3( 43,   0, -45), new Vertex3( 69,  -3,   0),
                new Vertex3( 43, -14,  28), new Vertex3(-43,   0, -45),
                new Vertex3(-69,  -3,   0), new Vertex3(-43, -14,  28),
                new Vertex3( 26,  -7,  73), new Vertex3(-26,  -7,  73),
                new Vertex3( 43,  14,  28), new Vertex3(-43,  14,  28),
                new Vertex3(  0,   9, -45), new Vertex3(-17,   0, -45),
                new Vertex3( 17,   0, -45), new Vertex3(  0,  -4, -45),
                new Vertex3(  0,   4, -45), new Vertex3(  0,  -7,  73),
                new Vertex3(  0,  -7,  83),
            },
            Edges = new List<Edge>
            {
                new Edge(0, 1), new Edge(0, 4), new Edge(0, 7),
                new Edge(1, 2), new Edge(2, 3), new Edge(3, 8),
                new Edge(8, 9), new Edge(6, 9), new Edge(5, 6),
                new Edge(1, 5), new Edge(3, 4), new Edge(4, 8),
                new Edge(6, 7), new Edge(7, 9), new Edge(2, 12),
                new Edge(5, 12), new Edge(10, 12), new Edge(11, 12),
                new Edge(10, 11), new Edge(6, 11), new Edge(9, 11),
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

        var worm = new ModelAnalysis
        {
            Name = "Worm",
            Vertices = new List<Vertex3>
            {
                new Vertex3(  0,   0,  35), new Vertex3(-26, -10, -15),
                new Vertex3( 26, -10, -15), new Vertex3(-26,  10, -15),
                new Vertex3( 26,  10, -15), new Vertex3(  0, -14,   5),
                new Vertex3(  0,  14,   5), new Vertex3(-15,  -7,  10),
                new Vertex3( 15,  -7,  10), new Vertex3(  0,   0, -15),
            },
            Edges = new List<Edge>
            {
                new Edge(0, 1), new Edge(0, 2), new Edge(0, 3), new Edge(0, 4),
                new Edge(1, 2), new Edge(3, 4), new Edge(1, 3), new Edge(2, 4),
                new Edge(0, 5), new Edge(0, 6), new Edge(5, 7), new Edge(5, 8),
                new Edge(1, 7), new Edge(2, 8), new Edge(3, 6), new Edge(4, 6),
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
