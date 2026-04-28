using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Gecko — small scout ship from Elite enhanced versions.
/// 12 vertices, 17 edges, 9 faces.
/// Compact and agile reconnaissance vessel.
/// </summary>
public class GeckoModel : ShipModel
{
    public static new GeckoModel Create(float size = 1.0f)
    {
        float scale = size / 100f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  50),   // 0 - nose
            (-40, -10, -20),   // 1 - port rear lower
            ( 40, -10, -20),   // 2 - starboard rear lower
            (-40,  10, -20),   // 3 - port rear upper
            ( 40,  10, -20),   // 4 - starboard rear upper
            (  0, -15,  10),   // 5 - lower center
            (  0,  15,  10),   // 6 - upper center
            (-20,  -8,   0),   // 7 - port lower mid
            ( 20,  -8,   0),   // 8 - starboard lower mid
            (-20,   8,   0),   // 9 - port upper mid
            ( 20,   8,   0),   // 10 - starboard upper mid
            (  0,   0, -20),   // 11 - rear center
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
            (1, 7, null),
            (2, 8, null),
            (3, 9, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 1, 3 }),
            new(new[] { 0, 3, 4, 2 }),
            new(new[] { 0, 2, 1 }),
            new(new[] { 0, 5, 8, 7 }),
            new(new[] { 0, 7, 9, 6 }),
            new(new[] { 0, 6, 10, 8 }),
            new(new[] { 1, 7, 5 }),
            new(new[] { 2, 8, 5 }),
            new(new[] { 3, 4, 11 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new GeckoModel
        {
            Name = "Gecko",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
