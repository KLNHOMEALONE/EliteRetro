using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Elite logo from Elite (1984).
/// 42 vertices, 37 edges, 5 faces.
/// The iconic "ELITE" text rendered as a 3D wireframe.
/// </summary>
public class LogoModel : ShipModel
{
    public static new LogoModel Create(float size = 1.0f)
    {
        float scale = size / 180f;

        // Original Elite coordinates — 42 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0,  -9,  55),   // 0
            ( -10, -9,  30),   // 1
            ( -25, -9,  93),   // 2
            (-150, -9, 180),   // 3
            ( -90, -9,  10),   // 4
            (-140, -9,  10),   // 5
            (  0,  -9, -95),   // 6
            (140,  -9,  10),   // 7
            ( 90,  -9,  10),   // 8
            (150,  -9, 180),   // 9
            ( 25,  -9,  93),   // 10
            ( 10,  -9,  30),   // 11
            ( -85, -9, -30),   // 12
            ( 85,  -9, -30),   // 13
            ( -70, 11,   5),   // 14
            ( -70, 11, -25),   // 15
            ( 70,  11, -25),   // 16
            ( 70,  11,   5),   // 17
            (  0,  -9,   5),   // 18 - duplicate center
            (  0,  -9,   5),   // 19 - duplicate center
            (  0,  -9,   5),   // 20 - duplicate center
            ( -28, 11,  -2),   // 21 - E
            ( -49, 11,  -2),   // 22
            ( -49, 11, -10),   // 23
            ( -49, 11, -17),   // 24
            ( -28, 11, -17),   // 25
            ( -28, 11, -10),   // 26
            ( -24, 11,  -2),   // 27
            ( -24, 11, -17),   // 28
            (  -3, 11, -17),   // 29
            (  0,  11,  -2),   // 30
            (  0,  11, -17),   // 31
            (  4,  11,  -2),   // 32
            ( 25,  11,  -2),   // 33
            ( 14,  11,  -2),   // 34
            ( 14,  11, -17),   // 35
            ( 49,  11,  -2),   // 36
            ( 28,  11,  -2),   // 37
            ( 28,  11, -10),   // 38
            ( 28,  11, -17),   // 39
            ( 49,  11, -17),   // 40
            ( 49,  11, -10),   // 41
        };

        // 37 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Outer border
            (0, 1, Color.Yellow),
            (1, 2, Color.Yellow),
            (2, 3, Color.Yellow),
            (3, 4, Color.Yellow),
            (4, 5, Color.Yellow),
            (5, 6, Color.Yellow),
            (6, 7, Color.Yellow),
            (7, 8, Color.Yellow),
            (8, 9, Color.Yellow),
            (9, 10, Color.Yellow),
            (10, 11, Color.Yellow),
            (11, 0, Color.Yellow),
            // Inner structure
            (14, 15, Color.Yellow),
            (15, 16, Color.Yellow),
            (16, 17, Color.Yellow),
            (17, 14, Color.Yellow),
            (4, 12, Color.Yellow),
            (12, 13, Color.Yellow),
            (13, 8, Color.Yellow),
            (8, 4, Color.Yellow),
            (4, 14, Color.Yellow),
            (12, 15, Color.Yellow),
            (13, 16, Color.Yellow),
            (8, 17, Color.Yellow),
            // Letter edges (E, L, I, T, E)
            (21, 22, Color.Yellow),
            (22, 24, Color.Yellow),
            (24, 25, Color.Yellow),
            (23, 26, Color.Yellow),
            (27, 28, Color.Yellow),
            (28, 29, Color.Yellow),
            (30, 31, Color.Yellow),
            (32, 33, Color.Yellow),
            (34, 35, Color.Yellow),
            (36, 37, Color.Yellow),
            (37, 39, Color.Yellow),
            (39, 40, Color.Yellow),
            (41, 38, Color.Yellow),
        };

        // 5 faces
        var faces = new Face[]
        {
            new(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }),  // 0 - main face
            new(new[] { 14, 15, 16, 17 }),                         // 1
            new(new[] { 4, 8, 13, 12 }),                           // 2
            new(new[] { 4, 12, 15, 14 }),                          // 3
            new(new[] { 8, 17, 16, 13 }),                          // 4
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Gold));

        return new LogoModel
        {
            Name = "Elite Logo",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
