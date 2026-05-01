using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Rock Hermit — asteroid-based mining station from Elite enhanced versions.
/// 9 vertices, 21 edges, 14 faces.
/// Shares geometry with Asteroid but with docking facilities.
/// </summary>
public class RockHermitModel : ShipModel
{
    public static new RockHermitModel Create(float size = 1.0f)
    {
        float scale = size / 150f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,  80,   0),   // 0 - top
            (-80, -10,   0),   // 1 - port side
            (  0, -80,   0),   // 2 - bottom
            ( 70, -40,   0),   // 3 - starboard side
            ( 60,  50,   0),   // 4 - upper starboard
            ( 50,   0,  60),   // 5 - front starboard
            (-40,   0,  70),   // 6 - front port
            (  0,  30, -75),   // 7 - rear top
            (  0, -50, -60),   // 8 - rear bottom
        };

        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, Color.Red),
            (0, 4, Color.Red),
            (3, 4, Color.Red),
            (2, 3, Color.Red),
            (1, 2, Color.Red),
            (1, 6, null),
            (2, 6, null),
            (2, 5, null),
            (5, 6, null),
            (0, 5, null),
            (3, 5, null),
            (0, 6, null),
            (4, 5, null),
            (1, 8, null),
            (1, 7, null),
            (0, 7, null),
            (4, 7, null),
            (3, 7, null),
            (3, 8, null),
            (2, 8, null),
            (7, 8, null),
            // Additional edges exposed by face boundary analysis
            (1, 4, null),
            (5, 8, null),
            (6, 7, null),
            (6, 8, null),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 1, 6, 5 }),
            new(new[] { 0, 5, 4 }),
            new(new[] { 1, 2, 6 }),
            new(new[] { 2, 3, 5, 6 }),
            new(new[] { 3, 4, 5 }),
            new(new[] { 4, 0, 1 }),
            new(new[] { 7, 6, 0 }),          // reversed
            new(new[] { 1, 7, 8 }),
            new(new[] { 6, 8, 2 }),          // reversed
            new(new[] { 8, 7, 3 }),          // reversed
            new(new[] { 4, 7, 0 }),
            new(new[] { 7, 4, 3 }),          // reversed
            new(new[] { 8, 5, 2 }),          // reversed
            new(new[] { 8, 3, 5 }),          // reversed
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Red));

        return new RockHermitModel
        {
            Name = "Rock Hermit",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
