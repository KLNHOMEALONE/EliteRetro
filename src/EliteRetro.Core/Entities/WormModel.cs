using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Worm — small scout/trader from Elite enhanced versions.
/// 10 vertices, 16 edges, 8 faces.
/// Tiny agile vessel used for scouting and light trading.
/// </summary>
public class WormModel : ShipModel
{
    public static new WormModel Create(float size = 1.0f)
    {
        float scale = size / 60f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  35),   // 0 - nose
            (-26, -10, -15),   // 1 - port rear lower
            ( 26, -10, -15),   // 2 - starboard rear lower
            (-26,  10, -15),   // 3 - port rear upper
            ( 26,  10, -15),   // 4 - starboard rear upper
            (  0, -14,   5),   // 5 - lower center
            (  0,  14,   5),   // 6 - upper center
            (-15,  -7,  10),   // 7 - port wing lower
            ( 15,  -7,  10),   // 8 - starboard wing lower
            (  0,   0, -15),   // 9 - rear center
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
            (0, 7, null),   // added - face boundary
            (0, 8, null),   // added - face boundary
            (1, 5, null),   // added - face boundary
            (2, 5, null),   // added - face boundary
            (7, 8, null),   // added - face boundary
            (6, 7, null),   // added - face boundary
            (6, 8, null),   // added - face boundary
            (1, 7, null),
            (2, 8, null),
            (3, 6, null),
            (4, 6, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 3, 1 }),          // F0 (reversed from {0,1,3})
            new(new[] { 0, 2, 4, 3 }),       // F1 (reversed from {0,3,4,2})
            new(new[] { 0, 1, 2 }),          // F2 (reversed from {0,2,1})
            new(new[] { 0, 7, 8, 5 }),       // F3 (reversed from {0,5,8,7})
            new(new[] { 0, 6, 7 }),          // F4 (reversed from {0,7,6})
            new(new[] { 0, 8, 6 }),          // F5 (reversed from {0,6,8})
            new(new[] { 1, 5, 7 }),          // F6 (reversed from {1,7,5})
            new(new[] { 2, 8, 5 }),          // F7 ✓
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new WormModel
        {
            Name = "Worm",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
