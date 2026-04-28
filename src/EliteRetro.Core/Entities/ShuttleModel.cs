using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Shuttle — civilian transport from Elite enhanced versions.
/// 19 vertices, 30 edges, 13 faces.
/// Slow but capacious civilian cargo/passenger transport.
/// </summary>
public class ShuttleModel : ShipModel
{
    public static new ShuttleModel Create(float size = 1.0f)
    {
        float scale = size / 80f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  35),   // 0 - nose
            (-20, -20, -15),   // 1 - port lower rear
            ( 20, -20, -15),   // 2 - starboard lower rear
            (-20,  20, -15),   // 3 - port upper rear
            ( 20,  20, -15),   // 4 - starboard upper rear
            (-30, -15,  10),   // 5 - port lower wing
            ( 30, -15,  10),   // 6 - starboard lower wing
            (-30,  15,  10),   // 7 - port upper wing
            ( 30,  15,  10),   // 8 - starboard upper wing
            (  0, -25,   0),   // 9 - bottom center
            (  0,  25,   0),   // 10 - top center
            (-10,   0,  25),   // 11 - port cockpit
            ( 10,   0,  25),   // 12 - starboard cockpit
            (-15, -18,  -5),   // 13 - port lower mid
            ( 15, -18,  -5),   // 14 - starboard lower mid
            (-15,  18,  -5),   // 15 - port upper mid
            ( 15,  18,  -5),   // 16 - starboard upper mid
            (  0, -20, -15),   // 17 - lower rear center
            (  0,  20, -15),   // 18 - upper rear center
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
            (0, 7, null),
            (0, 8, null),
            (5, 6, null),
            (7, 8, null),
            (1, 5, null),
            (2, 6, null),
            (3, 7, null),
            (4, 8, null),
            (9, 1, null),
            (9, 2, null),
            (10, 3, null),
            (10, 4, null),
            (11, 12, Color.Cyan),
            (5, 7, null),
            (6, 8, null),
            (13, 14, null),
            (15, 16, null),
            (17, 18, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 1, 3 }),
            new(new[] { 0, 3, 4, 2 }),
            new(new[] { 0, 2, 1 }),
            new(new[] { 0, 5, 7 }),
            new(new[] { 0, 7, 8, 6 }),
            new(new[] { 0, 6, 5 }),
            new(new[] { 1, 5, 6, 2 }),
            new(new[] { 3, 7, 8, 4 }),
            new(new[] { 1, 3, 7, 5 }),
            new(new[] { 2, 6, 8, 4 }),
            new(new[] { 9, 17, 18, 10 }),
            new(new[] { 11, 12, 0 }),
            new(new[] { 13, 14, 16, 15 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new ShuttleModel
        {
            Name = "Shuttle",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
