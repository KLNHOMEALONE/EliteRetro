using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Thargoid — alien warship from Elite (1984).
/// 20 vertices, 26 edges, 10 faces.
/// Powerful alien vessel with distinctive organic design.
/// </summary>
public class ThargoidModel : ShipModel
{
    public static new ThargoidModel Create(float size = 1.0f)
    {
        float scale = size / 160f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  80),   // 0 - nose
            (-80, -20, -20),   // 1 - port lower rear
            ( 80, -20, -20),   // 2 - starboard lower rear
            (-80,  20, -20),   // 3 - port upper rear
            ( 80,  20, -20),   // 4 - starboard upper rear
            (  0, -30,  30),   // 5 - lower center
            (  0,  30,  30),   // 6 - upper center
            (-60, -15,  20),   // 7 - port lower wing
            ( 60, -15,  20),   // 8 - starboard lower wing
            (-60,  15,  20),   // 9 - port upper wing
            ( 60,  15,  20),   // 10 - starboard upper wing
            (  0,   0, -40),   // 11 - rear center
            (-40,   0,  50),   // 12 - port cockpit
            ( 40,   0,  50),   // 13 - starboard cockpit
            (-30, -20,   0),   // 14 - port lower mid
            ( 30, -20,   0),   // 15 - starboard lower mid
            (-30,  20,   0),   // 16 - port upper mid
            ( 30,  20,   0),   // 17 - starboard upper mid
            (  0, -25, -20),   // 18 - lower rear
            (  0,  25, -20),   // 19 - upper rear
        };

        var edges = new (int a, int b, Color? c)[]
        {
            // Main hull edges (red for alien ship)
            (0, 1, Color.Red),
            (0, 2, Color.Red),
            (0, 3, Color.Red),
            (0, 4, Color.Red),
            (1, 2, null),
            (3, 4, null),
            (1, 3, Color.Red),
            (2, 4, Color.Red),
            (0, 5, null),
            (0, 6, null),
            (5, 7, null),
            (5, 8, null),
            (6, 9, null),
            (6, 10, null),
            (1, 7, null),
            (2, 8, null),
            (3, 9, null),
            (4, 10, null),
            (0, 7, null),   // added - face boundary
            (0, 8, null),   // added - face boundary
            (1, 5, null),   // added - face boundary
            (2, 5, null),   // added - face boundary
            (3, 6, null),   // added - face boundary
            (4, 6, null),   // added - face boundary
            (7, 9, null),   // added - face boundary
            (8, 10, null),  // added - face boundary
            (7, 8, null),
            (9, 10, null),
            (11, 1, Color.Red),
            (11, 2, Color.Red),
            (11, 3, Color.Red),
            (11, 4, Color.Red),
            (12, 13, Color.Cyan),
            (14, 15, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 3, 1 }),          // F0 (reversed from {0,1,3})
            new(new[] { 0, 2, 4, 3 }),       // F1 (reversed from {0,3,4,2})
            new(new[] { 0, 1, 2 }),          // F2 (reversed from {0,2,1})
            new(new[] { 0, 7, 8, 5 }),       // F3 (reversed from {0,5,8,7})
            new(new[] { 0, 6, 9, 7 }),       // F4 (reversed from {0,7,9,6})
            new(new[] { 0, 8, 10, 6 }),      // F5 (reversed from {0,6,10,8})
            new(new[] { 1, 5, 7 }),          // F6 (reversed from {1,7,5})
            new(new[] { 2, 8, 5 }),          // F7 ✓
            new(new[] { 3, 6, 9 }),          // F8 (reversed from {3,9,6})
            new(new[] { 4, 6, 10 }),         // F9 (reversed from {4,10,6})
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new ThargoidModel
        {
            Name = "Thargoid",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
