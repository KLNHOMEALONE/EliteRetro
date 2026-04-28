using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Splinter — tiny asteroid fragment from Elite enhanced versions.
/// 4 vertices, 6 edges, 4 faces.
/// Very small piece of space debris, often found in asteroid fields.
/// </summary>
public class SplinterModel : ShipModel
{
    public static new SplinterModel Create(float size = 1.0f)
    {
        float scale = size / 30f;

        var verts = new (int x, int y, int z)[]
        {
            ( 12,  25,  16),   // 0
            (-24, -10,   5),   // 1
            ( 10, -25, -10),   // 2
            ( -8,   5, -20),   // 3
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Red),
            (0, 2, Color.Red),
            (0, 3, Color.Red),
            (1, 2, null),
            (1, 3, null),
            (2, 3, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 1, 2 }),
            new(new[] { 0, 2, 3 }),
            new(new[] { 0, 3, 1 }),
            new(new[] { 1, 3, 2 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Red));

        return new SplinterModel
        {
            Name = "Splinter",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
