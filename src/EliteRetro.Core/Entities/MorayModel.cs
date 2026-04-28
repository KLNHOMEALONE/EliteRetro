using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Moray — patrol ship from Elite enhanced versions.
/// 14 vertices, 19 edges, 9 faces.
/// Agile patrol vessel with good maneuverability.
/// </summary>
public class MorayModel : ShipModel
{
    public static new MorayModel Create(float size = 1.0f)
    {
        float scale = size / 100f;

        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  50),   // 0 - nose
            (-40, -15, -10),   // 1 - port rear lower
            ( 40, -15, -10),   // 2 - starboard rear lower
            (-40,  15, -10),   // 3 - port rear upper
            ( 40,  15, -10),   // 4 - starboard rear upper
            (  0, -20,  20),   // 5 - lower center
            (  0,  20,  20),   // 6 - upper center
            (-25, -12,   5),   // 7 - port lower wing
            ( 25, -12,   5),   // 8 - starboard lower wing
            (-25,  12,   5),   // 9 - port upper wing
            ( 25,  12,   5),   // 10 - starboard upper wing
            (  0,   0, -10),   // 11 - rear center
            (-15,   0,  30),   // 12 - port cockpit
            ( 15,   0,  30),   // 13 - starboard cockpit
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
            new(new[] { 12, 13, 0 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new MorayModel
        {
            Name = "Moray",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
