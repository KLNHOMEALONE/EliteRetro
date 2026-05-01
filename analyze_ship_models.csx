using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// Vertex and helper types
record Vertex3(float X, float Y, float Z);
record Face(int[] Vertices);
record Edge(int A, int B);

class Analyzer
{
    static void Main()
    {
        Console.WriteLine("=== ESCAPE POD ===\n");
        AnalyzeModel(
            "Escape Pod",
            new (int x, int y, int z)[] { (-7,0,36), (-7,-14,-12), (-7,14,-12), (21,0,0) },
            new (int a, int b)[] { (3,0), (3,1), (3,2), (0,1), (1,2), (0,2) },
            new int[][] { new[] {1,2,3}, new[] {0,2,3}, new[] {0,1,3}, new[] {0,1,2} }
        );

        Console.WriteLine("\n=== SHUTTLE ===\n");
        AnalyzeModel(
            "Shuttle",
            new (int x, int y, int z)[] {
                (0,0,35), (-20,-20,-15), (20,-20,-15), (-20,20,-15), (20,20,-15),
                (-30,-15,10), (30,-15,10), (-30,15,10), (30,15,10), (0,-25,0),
                (0,25,0), (-10,0,25), (10,0,25), (-15,-18,-5), (15,-18,-5),
                (-15,18,-5), (15,18,-5), (0,-20,-15), (0,20,-15)
            },
            new (int a, int b)[] {
                (0,1),(0,2),(0,3),(0,4),(1,2),(3,4),(1,3),(2,4),
                (0,5),(0,6),(0,7),(0,8),(5,6),(7,8),(1,5),(2,6),(3,7),(4,8),
                (9,1),(9,2),(10,3),(10,4),(11,12),(5,7),(6,8),(13,14),(15,16),(17,18)
            },
            new int[][] {
                new[] {0,1,3}, new[] {0,3,4,2}, new[] {0,2,1}, new[] {0,5,7},
                new[] {0,7,8,6}, new[] {0,6,5}, new[] {1,5,6,2}, new[] {3,7,8,4},
                new[] {1,3,7,5}, new[] {2,6,8,4}, new[] {9,17,18,10}, new[] {11,12,0},
                new[] {13,14,16,15}
            }
        );

        Console.WriteLine("\n=== TRANSPORTER ===\n");
        AnalyzeModel(
            "Transporter",
            new (int x, int y, int z)[] {
                (-33,-8,-26), (33,-8,-26), (33,8,-26), (-33,8,-26),
                (-33,-8,30), (33,-8,30), (33,8,30), (-33,8,30),
                (-50,-10,0), (50,-10,0), (-50,10,0), (50,10,0),
                (0,-12,40), (0,12,40), (-20,-6,35), (20,-6,35), (-20,6,35), (20,6,35),
                (-33,0,0), (33,0,0), (0,-8,-10), (0,8,-10),
                (-15,-5,20), (15,-5,20), (-15,5,20), (15,5,20),
                (-40,-9,10), (40,-9,10), (-40,9,10), (40,9,10),
                (0,-10,50), (0,10,50), (-25,-7,45), (25,-7,45), (-25,7,45), (25,7,45),
                (0,0,55)
            },
            new (int a, int b)[] {
                (0,1),(1,2),(2,3),(3,0),(4,5),(5,6),(6,7),(7,4),
                (0,4),(1,5),(2,6),(3,7),
                (8,9),(10,11),(0,8),(1,9),(3,10),(2,11),
                (12,13),(4,12),(7,13),(5,12),(6,13),
                (14,15),(16,17),(14,16),(15,17),
                (18,19),(20,21),(22,23),(24,25),(26,27),(28,29),(30,31),(32,33),(34,35),
                (30,36),(31,36),
                (8,26),(9,27),(10,28),(11,29),(26,32),(27,33)
            },
            new int[][] {
                new[] {0,1,2,3}, new[] {4,5,6,7}, new[] {0,1,5,4}, new[] {1,2,6,5},
                new[] {2,3,7,6}, new[] {3,0,4,7}, new[] {8,9,11,10}, new[] {0,8,10,3},
                new[] {1,9,11,2}, new[] {12,13,7,4}, new[] {14,15,17,16}, new[] {22,23,25,24},
                new[] {30,31,36}, new[] {32,33,35,34}
            }
        );
    }

    static void AnalyzeModel(string name, (int x, int y, int z)[] verts, (int a, int b)[] edges, int[][] faces)
    {
        var V = verts.Select(v => new Vertex3(v.x, v.y, v.z)).ToArray();
        var edgeSet = new HashSet<(int, int)>();
        foreach (var e in edges)
        {
            edgeSet.Add((Math.Min(e.a, e.b), Math.Max(e.a, e.b)));
        }

        Console.WriteLine($"Vertices ({V.Length}):");
        for (int i = 0; i < V.Length; i++)
            Console.WriteLine($"  {i}: ({V[i].X,4}, {V[i].Y,4}, {V[i].Z,4})");

        Console.WriteLine($"\nEdges ({edges.Length}):");
        foreach (var e in edges)
            Console.WriteLine($"  ({e.a}, {e.b})");

        Console.WriteLine($"\nFace analysis:");
        var correctedFaces = new List<int[]>();
        var missingEdges = new List<string>();

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            var faceVerts = face.Select(i => V[i]).ToArray();

            // Compute centroid
            float cx = faceVerts.Average(v => v.X);
            float cy = faceVerts.Average(v => v.Y);
            float cz = faceVerts.Average(v => v.Z);
            var centroid = new Vertex3(cx, cy, cz);

            // Compute normal from first 3 vertices
            var v0 = faceVerts[0];
            var v1 = faceVerts[1];
            var v2 = faceVerts[2];

            var edge1 = new Vertex3(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
            var edge2 = new Vertex3(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);

            // Cross product
            var normal = new Vertex3(
                edge1.Y * edge2.Z - edge1.Z * edge2.Y,
                edge1.Z * edge2.X - edge1.X * edge2.Z,
                edge1.X * edge2.Y - edge1.Y * edge2.X
            );

            // Dot with centroid
            float dot = normal.X * centroid.X + normal.Y * centroid.Y + normal.Z * centroid.Z;

            bool outward = dot > 0;
            string status = outward ? "OUTWARD" : "INWARD (needs reversal)";

            Console.WriteLine($"  Face {f}: [{string.Join(", ", face)}]");
            Console.WriteLine($"    Centroid: ({cx:F2}, {cy:F2}, {cz:F2})");
            Console.WriteLine($"    Normal: ({normal.X:F2}, {normal.Y:F2}, {normal.Z:F2})");
            Console.WriteLine($"    Dot(normal, centroid): {dot:F2} -> {status}");

            if (!outward)
            {
                var reversed = face.Reverse().ToArray();
                Console.WriteLine($"    CORRECTED: [{string.Join(", ", reversed)}]");
                correctedFaces.Add(reversed);
            }
            else
            {
                correctedFaces.Add(face.ToArray());
            }

            // Check boundary edges
            for (int i = 0; i < face.Length; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % face.Length];
                var key = (Math.Min(a, b), Math.Max(a, b));
                if (!edgeSet.Contains(key))
                {
                    missingEdges.Add($"({a},{b})");
                    Console.WriteLine($"    MISSING EDGE: ({a},{b})");
                }
            }
        }

        if (missingEdges.Count == 0)
        {
            Console.WriteLine("\n  All face boundary edges are present in edge list.");
        }
        else
        {
            Console.WriteLine($"\n  Missing edges: {string.Join(", ", missingEdges)}");
        }

        Console.WriteLine($"\n  Corrected face definitions:");
        for (int f = 0; f < correctedFaces.Count; f++)
        {
            Console.WriteLine($"    new(new[] {{ {string.Join(", ", correctedFaces[f])} }}),");
        }
    }
}
