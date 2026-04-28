using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Boa — large trader ship from Elite (1984).
/// 13 vertices, 24 edges, 13 faces.
/// Bulky cargo hauler with substantial cargo capacity.
/// </summary>
public class BoaModel : ShipModel
{
    public static new BoaModel Create(float size = 1.0f)
    {
        float scale = size / 200f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0, 100),   // 0 - nose
            (-62, -65,-107),   // 1 - port lower rear
            ( 62, -65,-107),   // 2 - starboard lower rear
            (-62,  40, -93),   // 3 - port upper rear
            ( 62,  40, -93),   // 4 - starboard upper rear
            (-30, -40,  50),   // 5 - port lower mid
            ( 30, -40,  50),   // 6 - starboard lower mid
            (-30,  25,  50),   // 7 - port upper mid
            ( 30,  25,  50),   // 8 - starboard upper mid
            (  0, -50,-100),   // 9 - bottom rear
            (  0,  35, -90),   // 10 - top rear
            (-10,   0,  80),   // 11 - cockpit port
            ( 10,   0,  80),   // 12 - cockpit starboard
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
            (5, 7, null),
            (6, 8, null),
            (1, 5, null),
            (2, 6, null),
            (3, 7, null),
            (4, 8, null),
            (9, 1, null),
            (9, 2, null),
            (10, 3, null),
            (10, 4, null),
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
            new(new[] { 5, 9, 6 }),
            new(new[] { 7, 10, 8 }),
            new(new[] { 11, 12, 0 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new BoaModel
        {
            Name = "Boa",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
