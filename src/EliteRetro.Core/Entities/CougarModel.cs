using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Cougar — rare easter egg ship from Elite (1984 enhanced versions).
/// 19 vertices, 25 edges, 6 faces.
/// Extremely rare ship with ~0.011% spawn rate (1 in 9000).
/// Features a cloaking device in some versions.
/// </summary>
public class CougarModel : ShipModel
{
    public static new CougarModel Create(float size = 1.0f)
    {
        float scale = size / 120f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  67),   // 0 - nose
            (-60, -14, -40),   // 1 - port lower rear
            ( 60, -14, -40),   // 2 - starboard lower rear
            (-60,  14, -40),   // 3 - port upper rear
            ( 60,  14, -40),   // 4 - starboard upper rear
            (  0, -20,  20),   // 5 - lower center
            (  0,  20,  20),   // 6 - upper center
            (-40, -10,  10),   // 7 - port lower wing
            ( 40, -10,  10),   // 8 - starboard lower wing
            (-40,  10,  10),   // 9 - port upper wing
            ( 40,  10,  10),   // 10 - starboard upper wing
            (  0,   0, -40),   // 11 - rear center
            (-25,  -7,  40),   // 12 - port cockpit
            ( 25,  -7,  40),   // 13 - starboard cockpit
            (-20,   0,  50),   // 14 - nose detail
            ( 20,   0,  50),   // 15 - nose detail
            (  0, -15,   0),   // 16 - lower mid
            (  0,  15,   0),   // 17 - upper mid
            (  0,   0,  30),   // 18 - cockpit center
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Cyan),
            (0, 2, Color.Cyan),
            (0, 3, Color.Cyan),
            (0, 4, Color.Cyan),
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
            (1, 7, null),
            (2, 8, null),
            (3, 9, null),
            (4, 10, null),
            (0, 7, null),   // added - face boundary
            (0, 8, null),   // added - face boundary
            (7, 9, null),   // added - face boundary
            (8, 10, null),  // added - face boundary
            (7, 8, null),
            (9, 10, null),
            (11, 1, null),
            (11, 2, null),
            (11, 3, null),
            (11, 4, null),
            (12, 13, Color.Cyan),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 3, 1 }),          // F0 (reversed from {0,1,3})
            new(new[] { 0, 2, 4, 3 }),       // F1 (reversed from {0,3,4,2})
            new(new[] { 0, 1, 2 }),          // F2 (reversed from {0,2,1})
            new(new[] { 0, 7, 8, 5 }),       // F3 (reversed from {0,5,8,7})
            new(new[] { 0, 6, 9, 7 }),       // F4 (reversed from {0,7,9,6})
            new(new[] { 0, 8, 10, 6 }),      // F5 (reversed from {0,6,10,8})
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Cyan));

        return new CougarModel
        {
            Name = "Cougar",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
