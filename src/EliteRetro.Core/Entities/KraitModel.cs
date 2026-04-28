using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Krait — medium pirate ship from Elite enhanced versions.
/// 17 vertices, 21 edges, 6 faces.
/// Dangerous pirate vessel with good cargo space.
/// </summary>
public class KraitModel : ShipModel
{
    public static new KraitModel Create(float size = 1.0f)
    {
        float scale = size / 180f;

        var verts = new (int x, int y, int z)[]
        {
            (  0, -18,   0),   // 0 - lower nose
            (  0,  -9, -45),   // 1 - lower mid
            ( 43,   0, -45),   // 2 - starboard mid
            ( 69,  -3,   0),   // 3 - starboard nose
            ( 43, -14,  28),   // 4 - starboard front
            (-43,   0, -45),   // 5 - port mid
            (-69,  -3,   0),   // 6 - port nose
            (-43, -14,  28),   // 7 - port front
            ( 26,  -7,  73),   // 8 - starboard wing
            (-26,  -7,  73),   // 9 - port wing
            ( 43,  14,  28),   // 10 - starboard upper front
            (-43,  14,  28),   // 11 - port upper front
            (  0,   9, -45),   // 12 - upper mid
            (-17,   0, -45),   // 13 - port upper mid detail
            ( 17,   0, -45),   // 14 - starboard upper mid detail
            (  0,  -4, -45),   // 15 - center detail
            (  0,   4, -45),   // 16 - center upper detail
            (  0,  -7,  73),   // 17 - rear lower
            (  0,  -7,  83),   // 18 - rear lower extended
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Orange),
            (0, 4, Color.Orange),
            (0, 7, Color.Orange),
            (1, 2, null),
            (2, 3, null),
            (3, 8, null),
            (8, 9, null),
            (6, 9, null),
            (5, 6, null),
            (1, 5, null),
            (3, 4, null),
            (4, 8, null),
            (6, 7, null),
            (7, 9, null),
            (2, 12, null),
            (5, 12, null),
            (10, 12, null),
            (11, 12, null),
            (10, 11, null),
            (6, 11, null),
            (9, 11, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 1, 2, 4 }),
            new(new[] { 0, 4, 7 }),
            new(new[] { 0, 7, 1 }),
            new(new[] { 1, 5, 12 }),
            new(new[] { 1, 12, 2 }),
            new(new[] { 0, 1, 5, 7 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new KraitModel
        {
            Name = "Krait",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
