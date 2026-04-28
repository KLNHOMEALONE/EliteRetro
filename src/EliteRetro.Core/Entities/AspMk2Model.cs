using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Asp Mk II — fast pirate ship from Elite (1984).
/// 19 vertices, 28 edges, 12 faces.
/// Sleek design with high speed and strong weapons.
/// </summary>
public class AspMk2Model : ShipModel
{
    public static new AspMk2Model Create(float size = 1.0f)
    {
        float scale = size / 138f;

        var verts = new (int x, int y, int z)[]
        {
            (-18,   0,  40),   // 0 - port nose
            ( 18,   0,  40),   // 1 - starboard nose
            ( 30,   0, -24),   // 2 - starboard mid
            ( 30,   0, -40),   // 3 - starboard rear
            ( 18,  -7, -40),   // 4 - starboard lower rear
            (-18,  -7, -40),   // 5 - port lower rear
            (-30,   0, -40),   // 6 - port rear
            (-30,   0, -24),   // 7 - port mid
            (-18,   7, -40),   // 8 - port upper rear
            ( 18,   7, -40),   // 9 - starboard upper rear
            (-18,   7,  13),   // 10 - port upper front
            ( 18,   7,  13),   // 11 - starboard upper front
            (-18,  -7,  13),   // 12 - port lower front
            ( 18,  -7,  13),   // 13 - starboard lower front
            (-11,   3,  29),   // 14 - port canopy
            ( 11,   3,  29),   // 15 - starboard canopy
            ( 11,   4,  24),   // 16 - starboard canopy detail
            (-11,   4,  24),   // 17 - port canopy detail
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Orange),
            (1, 2, null),
            (2, 3, null),
            (3, 4, null),
            (4, 5, null),
            (5, 6, null),
            (6, 7, null),
            (7, 0, null),
            (3, 9, null),
            (9, 8, null),
            (8, 6, null),
            (0, 10, null),
            (7, 10, null),
            (1, 11, null),
            (2, 11, null),
            (0, 12, null),
            (7, 12, null),
            (1, 13, null),
            (2, 13, null),
            (10, 11, null),
            (12, 13, null),
            (8, 10, null),
            (9, 11, null),
            (5, 12, null),
            (4, 13, null),
            (14, 15, Color.Cyan),
            (15, 16, Color.Cyan),
            (16, 17, Color.Cyan),
            (17, 14, Color.Cyan),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 1, 11, 10 }),
            new(new[] { 0, 12, 13, 1 }),
            new(new[] { 1, 2, 3, 4, 13 }),
            new(new[] { 1, 13, 4, 3, 2 }),
            new(new[] { 10, 11, 9, 8 }),
            new(new[] { 12, 10, 8, 5 }),
            new(new[] { 3, 4, 5, 6 }),
            new(new[] { 7, 6, 5, 12, 10 }),
            new(new[] { 2, 11, 9, 3 }),
            new(new[] { 7, 10, 11, 2 }),
            new(new[] { 6, 8, 9, 3, 2, 7 }),
            new(new[] { 14, 15, 16, 17 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new AspMk2Model
        {
            Name = "Asp Mk II",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
