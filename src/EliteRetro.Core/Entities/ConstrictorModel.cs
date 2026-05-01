using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Constrictor — fast attack ship from Elite enhanced versions.
/// 17 vertices, 24 edges, 10 faces.
/// High-speed interceptor with powerful weapons.
/// </summary>
public class ConstrictorModel : ShipModel
{
    public static new ConstrictorModel Create(float size = 1.0f)
    {
        float scale = size / 120f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  80),   // 0 - nose
            (-54, -7, -40),   // 1 - port lower rear
            ( 54, -7, -40),   // 2 - starboard lower rear
            (-54,  13, -40),  // 3 - port upper rear
            ( 54,  13, -40),  // 4 - starboard upper rear
            (  0, -20,  40),  // 5 - lower nose
            (  0,  20,  40),  // 6 - upper nose
            (-30, -15,  20),  // 7 - port lower wing
            ( 30, -15,  20),  // 8 - starboard lower wing
            (-30,  15,  20),  // 9 - port upper wing
            ( 30,  15,  20),  // 10 - starboard upper wing
            (  0,   0, -40),  // 11 - rear center
            (-20,  -8,   0),  // 12 - port mid lower
            ( 20,  -8,   0),  // 13 - starboard mid lower
            (-20,   8,   0),  // 14 - port mid upper
            ( 20,   8,   0),  // 15 - starboard mid upper
            (  0,   0,  20),  // 16 - cockpit
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Orange),
            (0, 2, Color.Orange),
            (0, 3, Color.Orange),
            (0, 4, Color.Orange),
            (1, 2, null),
            (3, 4, null),
            (1, 3, null),
            (2, 4, null),
            (0, 5, null),
            (0, 6, null),
            (5, 7, null),
            (5, 8, null),
            (6, 9, null),
            (6, 10, null),
            (7, 8, null),
            (9, 10, null),
            (0, 7, null),   // added - face boundary
            (0, 8, null),   // added - face boundary
            (7, 9, null),   // added - face boundary
            (7, 11, null),  // added - face boundary
            (8, 10, null),  // added - face boundary
            (8, 11, null),  // added - face boundary
            (9, 11, null),  // added - face boundary
            (10, 11, null), // added - face boundary
            (1, 7, null),
            (2, 8, null),
            (3, 9, null),
            (4, 10, null),
            (11, 1, null),
            (11, 2, null),
            (11, 3, null),
            (11, 4, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 3, 1 }),          // F0 (reversed from {0,1,3})
            new(new[] { 0, 2, 4, 3 }),       // F1 (reversed from {0,3,4,2})
            new(new[] { 0, 1, 2 }),          // F2 (reversed from {0,2,1})
            new(new[] { 0, 7, 8, 5 }),       // F3 (reversed from {0,5,8,7})
            new(new[] { 0, 6, 9, 7 }),       // F4 ✓
            new(new[] { 0, 8, 10, 6 }),      // F5 (reversed from {0,6,10,8})
            new(new[] { 1, 11, 7 }),         // F6 (reversed from {1,7,11})
            new(new[] { 2, 11, 8 }),         // F7 (reversed from {2,8,11})
            new(new[] { 3, 9, 11 }),         // F8 (reversed from {3,11,9})
            new(new[] { 4, 11, 10 }),        // F9 (reversed from {4,10,11})
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new ConstrictorModel
        {
            Name = "Constrictor",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
