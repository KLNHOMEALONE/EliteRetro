using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Cobra Mk I — pirate variant of the Cobra series from Elite enhanced versions.
/// 11 vertices, 18 edges, 10 faces.
/// Smaller predecessor to the Cobra Mk III.
/// </summary>
public class CobraMk1Model : ShipModel
{
    public static new CobraMk1Model Create(float size = 1.0f)
    {
        float scale = size / 132f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  60),   // 0 - nose
            (-66, -12, -38),   // 1 - port rear lower
            ( 66, -12, -38),   // 2 - starboard rear lower
            (-66,  12, -38),   // 3 - port rear upper
            ( 66,  12, -38),   // 4 - starboard rear upper
            (  0, -20,  20),   // 5 - lower center
            (  0,  20,  20),   // 6 - upper center
            (-30, -10,  10),   // 7 - port lower wing
            ( 30, -10,  10),   // 8 - starboard lower wing
            (-30,  10,  10),   // 9 - port upper wing
            ( 30,  10,  10),   // 10 - starboard upper wing
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
            (1, 7, null),
            (2, 8, null),
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
            new(new[] { 3, 9, 6 }),
            new(new[] { 4, 10, 6 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new CobraMk1Model
        {
            Name = "Cobra Mk I",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
