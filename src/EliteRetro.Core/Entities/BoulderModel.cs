using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Boulder — small asteroid. 7 vertices, 15 edges, 10 triangular faces.
/// Convex hull with pre-computed outward normals for correct back-face culling.
/// </summary>
public class BoulderModel : ShipModel
{
    public static new BoulderModel Create(float size = 1.0f)
    {
        float scale = size / 60f;

        var verts = new (int x, int y, int z)[]
        {
            ( 30,  20,  10),   // 0: top-right-front
            (-25,  25, -10),   // 1: top-left-back
            ( 20, -15, -30),   // 2: bottom-right-back
            (-20, -25,  15),   // 3: bottom-left-front
            (-30, -10, -25),   // 4: bottom-left-back
            ( 25, -20, -20),   // 5: bottom-right-back (lower)
            (  0,  30,  35),   // 6: top peak
        };

        var edges = new (int a, int b)[]
        {
            (0, 1), (0, 2), (0, 3), (0, 5), (0, 6),
            (1, 2), (1, 3), (1, 4), (1, 6),
            (2, 4), (2, 5),
            (3, 4), (3, 5), (3, 6),
            (4, 5),
        };

        // Faces with pre-computed outward normals (CCW vertex order)
        var faces = new Face[]
        {
            new(new[] { 0, 1, 6 }, new Vector3(  325,  1975,  -400)),  // F0: top-right
            new(new[] { 0, 6, 3 }, new Vector3( 1175, -1100,  1850)),  // F1: front
            new(new[] { 0, 3, 5 }, new Vector3( 1550, -1525,  1775)),  // F2: front-bottom
            new(new[] { 0, 5, 2 }, new Vector3(  550,   100,  -225)),  // F3: right
            new(new[] { 0, 2, 1 }, new Vector3(  900,  2000, -1975)),  // F4: top-back
            new(new[] { 1, 2, 4 }, new Vector3( -100,   775, -1775)),  // F5: left-back-top
            new(new[] { 1, 4, 3 }, new Vector3(-1625,   -50,   425)),  // F6: left
            new(new[] { 1, 3, 6 }, new Vector3(-2375,   400,  1275)),  // F7: left-top-front
            new(new[] { 2, 5, 4 }, new Vector3(  -75,  -525,  -225)),  // F8: bottom-back
            new(new[] { 3, 4, 5 }, new Vector3( -325, -2150,  -725)),  // F9: bottom
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b) in edges)
            edgeList.Add(new Edge(a, b, Color.Gray));

        return new BoulderModel
        {
            Name = "Boulder",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
            IsRock = true,
        };
    }
}
