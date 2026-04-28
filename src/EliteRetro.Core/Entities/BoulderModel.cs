using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Boulder — small asteroid from Elite enhanced versions.
/// 7 vertices, 15 edges, 10 faces.
/// Small rocky space debris.
/// </summary>
public class BoulderModel : ShipModel
{
    public static new BoulderModel Create(float size = 1.0f)
    {
        float scale = size / 60f;

        var verts = new (int x, int y, int z)[]
        {
            ( 30,  10,  13),   // 0
            (-28,  37,  -9),   // 1
            ( 15, -10, -39),   // 2
            (-10, -30,  10),   // 3
            (-35, -15, -20),   // 4
            ( 20, -25, -25),   // 5
            (  0,  20,  40),   // 6
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Red),
            (0, 2, Color.Red),
            (0, 3, Color.Red),
            (0, 5, Color.Red),
            (1, 2, null),
            (1, 3, null),
            (1, 4, null),
            (1, 6, null),
            (2, 3, null),
            (2, 5, null),
            (3, 4, null),
            (4, 5, null),
            (4, 6, null),
            (5, 6, null),
            (0, 6, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 1, 2 }),
            new(new[] { 0, 2, 3 }),
            new(new[] { 0, 3, 1 }),
            new(new[] { 1, 3, 4 }),
            new(new[] { 1, 4, 6 }),
            new(new[] { 2, 3, 5 }),
            new(new[] { 3, 4, 5 }),
            new(new[] { 4, 5, 6 }),
            new(new[] { 5, 2, 6 }),
            new(new[] { 0, 5, 6 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Red));

        return new BoulderModel
        {
            Name = "Boulder",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
