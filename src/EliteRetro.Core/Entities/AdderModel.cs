using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Adder — light trader ship from Elite (1984).
/// 18 vertices, 29 edges, 15 faces.
/// Elongated hull with distinctive cockpit bubble.
/// </summary>
public class AdderModel : ShipModel
{
    public static new AdderModel Create(float size = 1.0f)
    {
        float scale = size / 108f;

        var verts = new (int x, int y, int z)[]
        {
            (  0, -14, 108),   // 0 - nose
            (-40, -14,  -4),   // 1 - port rear lower
            (-12, -14, -52),   // 2 - port engine lower
            ( 12, -14, -52),   // 3 - starboard engine lower
            ( 40, -14,  -4),   // 4 - starboard rear lower
            (-40,  14,  -4),   // 5 - port rear upper
            (-12,   2, -52),   // 6 - port engine upper
            ( 12,   2, -52),   // 7 - starboard engine upper
            ( 40,  14,  -4),   // 8 - starboard rear upper
            (  0,  18, -20),   // 9 - top ridge
            ( -3, -11,  97),   // 10 - cockpit port
            (-26,   8,  18),   // 11 - port wing
            (-16,  14,  -4),   // 12 - port top
            (  3, -11,  97),   // 13 - cockpit starboard
            ( 26,   8,  18),   // 14 - starboard wing
            ( 16,  14,  -4),   // 15 - starboard top
            (  0, -14, -20),   // 16 - rear bottom center
            (-14, -14,  44),   // 17 - port rear bottom
            ( 14, -14,  44),   // 18 - starboard rear bottom
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Orange),
            (1, 2, Color.Orange),
            (2, 3, Color.Orange),
            (3, 4, Color.Orange),
            (0, 4, Color.Orange),
            (0, 5, null),
            (1, 4, null),   // added - missing face boundary edge (F9)
            (4, 5, null),   // added - missing face boundary edge (F1)
            (5, 6, null),
            (6, 7, null),
            (7, 8, null),
            (0, 8, null),
            (5, 9, null),
            (6, 9, null),
            (7, 9, null),
            (8, 9, null),
            (1, 5, null),
            (2, 6, null),
            (3, 7, null),
            (4, 8, null),
            (10, 11, Color.Cyan),
            (11, 12, Color.Cyan),
            (10, 12, Color.Cyan),
            (13, 14, Color.Cyan),
            (14, 15, Color.Cyan),
            (13, 15, Color.Cyan),
            (16, 17, null),
            (16, 18, null),
            (17, 18, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 5, 1 }),          // F0 (reversed from {0,1,5})
            new(new[] { 0, 5, 4 }),          // F1 ✓
            new(new[] { 1, 5, 6, 2 }),       // F2 (reversed from {1,2,6,5})
            new(new[] { 2, 6, 7, 3 }),       // F3 (reversed from {2,3,7,6})
            new(new[] { 3, 7, 8, 4 }),       // F4 (reversed from {3,4,8,7})
            new(new[] { 0, 4, 8 }),          // F5 ✓
            new(new[] { 5, 9, 6 }),          // F6 (reversed from {5,6,9})
            new(new[] { 6, 9, 7 }),          // F7 (reversed from {6,7,9})
            new(new[] { 7, 9, 8 }),          // F8 (reversed from {7,8,9})
            new(new[] { 0, 1, 4 }),          // F9 ✓
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new AdderModel
        {
            Name = "Adder",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
