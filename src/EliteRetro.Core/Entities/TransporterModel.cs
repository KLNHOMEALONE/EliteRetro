using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Transporter — large cargo hauler from Elite enhanced versions.
/// 37 vertices, 52 edges, 14 faces.
/// Massive civilian freighter with huge cargo capacity.
/// </summary>
public class TransporterModel : ShipModel
{
    public static new TransporterModel Create(float size = 1.0f)
    {
        float scale = size / 100f;

        var verts = new (int x, int y, int z)[]
        {
            (-33,  -8, -26),   // 0
            ( 33,  -8, -26),   // 1
            ( 33,   8, -26),   // 2
            (-33,   8, -26),   // 3
            (-33,  -8,  30),   // 4
            ( 33,  -8,  30),   // 5
            ( 33,   8,  30),   // 6
            (-33,   8,  30),   // 7
            (-50, -10,   0),   // 8 - port wing
            ( 50, -10,   0),   // 9 - starboard wing
            (-50,  10,   0),   // 10 - port upper wing
            ( 50,  10,   0),   // 11 - starboard upper wing
            (  0, -12,  40),   // 12 - nose lower
            (  0,  12,  40),   // 13 - nose upper
            (-20,  -6,  35),   // 14
            ( 20,  -6,  35),   // 15
            (-20,   6,  35),   // 16
            ( 20,   6,  35),   // 17
            // Additional structural vertices
            (-33,   0,   0),   // 18
            ( 33,   0,   0),   // 19
            (  0,  -8, -10),   // 20
            (  0,   8, -10),   // 21
            (-15,  -5,  20),   // 22
            ( 15,  -5,  20),   // 23
            (-15,   5,  20),   // 24
            ( 15,   5,  20),   // 25
            (-40,  -9,  10),   // 26
            ( 40,  -9,  10),   // 27
            (-40,   9,  10),   // 28
            ( 40,   9,  10),   // 29
            (  0, -10,  50),   // 30
            (  0,  10,  50),   // 31
            (-25,  -7,  45),   // 32
            ( 25,  -7,  45),   // 33
            (-25,   7,  45),   // 34
            ( 25,   7,  45),   // 35
            (  0,   0,  55),   // 36 - nose tip
        };

        var edges = new (int a, int b, Color? c)[]
        {
            // Main hull
            (0, 1, Color.Orange),
            (1, 2, Color.Orange),
            (2, 3, Color.Orange),
            (3, 0, Color.Orange),
            (4, 5, Color.Orange),
            (5, 6, Color.Orange),
            (6, 7, Color.Orange),
            (7, 4, Color.Orange),
            (0, 4, null),
            (1, 5, null),
            (2, 6, null),
            (3, 7, null),
            // Wings
            (8, 9, null),
            (10, 11, null),
            (0, 8, null),
            (1, 9, null),
            (3, 10, null),
            (2, 11, null),
            // Nose
            (12, 13, null),
            (4, 12, null),
            (7, 13, null),
            (5, 12, null),
            (6, 13, null),
            // Cockpit details
            (14, 15, Color.Cyan),
            (16, 17, Color.Cyan),
            (14, 16, Color.Cyan),
            (15, 17, Color.Cyan),
            // Additional structure
            (18, 19, null),
            (20, 21, null),
            (22, 23, null),
            (24, 25, null),
            (26, 27, null),
            (28, 29, null),
            (30, 31, null),
            (32, 33, null),
            (34, 35, null),
            (30, 36, null),
            (31, 36, null),
            (8, 26, null),
            (9, 27, null),
            (10, 28, null),
            (11, 29, null),
            (26, 32, null),
            (27, 33, null),
            // Additional edges needed for face boundaries
            (9, 11, null),
            (10, 8, null),
            (23, 25, null),
            (24, 22, null),
            (33, 35, null),
            (34, 32, null),
        };

        var faces = new Face[]
        {
            new(new[] { 3, 2, 1, 0 }),     // 0 - reversed from [0,1,2,3]
            new(new[] { 4, 5, 6, 7 }),     // 1 - outward OK
            new(new[] { 0, 1, 5, 4 }),     // 2 - outward OK
            new(new[] { 1, 2, 6, 5 }),     // 3 - outward OK
            new(new[] { 2, 3, 7, 6 }),     // 4 - outward OK
            new(new[] { 3, 0, 4, 7 }),     // 5 - outward OK
            new(new[] { 10, 11, 9, 8 }),   // 6 - reversed from [8,9,11,10]
            new(new[] { 0, 8, 10, 3 }),    // 7 - outward OK
            new(new[] { 2, 11, 9, 1 }),    // 8 - reversed from [1,9,11,2]
            new(new[] { 12, 13, 7, 4 }),   // 9 - outward OK
            new(new[] { 14, 15, 17, 16 }), // 10 - outward OK
            new(new[] { 22, 23, 25, 24 }), // 11 - outward OK
            new(new[] { 36, 31, 30 }),     // 12 - reversed from [30,31,36]
            new(new[] { 32, 33, 35, 34 }), // 13 - outward OK
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new TransporterModel
        {
            Name = "Transporter",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
