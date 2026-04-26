using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Fer-de-Lance — deadly fighter from Elite (1984).
/// 19 vertices, 27 edges, 10 faces.
/// Sharp arrow-like design with forward-swept wings.
/// </summary>
public class FerDeLanceModel : ShipModel
{
    public static new FerDeLanceModel Create(float size = 1.0f)
    {
        float scale = size / 108f;

        // Original Elite coordinates — 19 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0, -14, 108),   // 0  - nose tip
            (-40, -14,  -4),   // 1  - left wing root front
            (-12, -14, -52),   // 2  - left rear bottom
            ( 12, -14, -52),   // 3  - right rear bottom
            ( 40, -14,  -4),   // 4  - right wing root front
            (-40,  14,  -4),   // 5  - left wing top
            (-12,   2, -52),   // 6  - left rear inner
            ( 12,   2, -52),   // 7  - right rear inner
            ( 40,  14,  -4),   // 8  - right wing top
            (  0,  18, -20),   // 9  - top ridge
            ( -3, -11,  97),   // 10 - nose left bottom
            (-26,   8,  18),   // 11 - left wing front
            (-16,  14,  -4),   // 12 - left wing tip
            (  3, -11,  97),   // 13 - nose right bottom
            ( 26,   8,  18),   // 14 - right wing front
            ( 16,  14,  -4),   // 15 - right wing tip
            (  0, -14, -20),   // 16 - bottom rear center
            (-14, -14,  44),   // 17 - bottom left front
            ( 14, -14,  44),   // 18 - bottom right front
        };

        // 27 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (0, 1, Color.Orange),   // 0
            (0, 4, Color.Orange),   // 4
            (0, 5, Color.Orange),   // 5
            (0, 8, Color.Orange),   // 9
            (0, 10, Color.Orange),  // nose left bottom
            (0, 13, Color.Orange),  // nose right bottom
            (0, 17, Color.Orange),  // bottom left front
            (0, 18, Color.Orange),  // bottom right front
            // Main hull
            (1, 2, null),   // 1
            (2, 3, null),   // 2
            (3, 4, null),   // 3
            (5, 6, null),   // 6
            (6, 7, null),   // 7
            (7, 8, null),   // 8
            (5, 9, null),   // 10
            (6, 9, null),   // 11
            (7, 9, null),   // 12
            (8, 9, null),   // 13
            (1, 5, null),   // 14
            (2, 6, null),   // 15
            (3, 7, null),   // 16
            (4, 8, null),   // 17
            // Nose detail edges (orange)
            (10, 11, Color.Orange),   // 18
            (11, 12, Color.Orange),   // 19
            (10, 12, Color.Orange),   // 20
            (13, 14, Color.Orange),   // 21
            (14, 15, Color.Orange),   // 22
            (13, 15, Color.Orange),   // 23
            // Bottom edges (dimmer)
            (16, 17, Color.DarkGreen),   // 24
            (16, 18, Color.DarkGreen),   // 25
            (17, 18, Color.DarkGreen),   // 26
        };

        // 10 faces
        var faces = new Face[]
        {
            new(new[] { 0, 5, 1 }),          // 0
            new(new[] { 0, 1, 6, 5 }),       // 1
            new(new[] { 1, 2, 6 }),          // 2
            new(new[] { 2, 3, 7, 6 }),       // 3
            new(new[] { 3, 4, 8, 7 }),       // 4
            new(new[] { 0, 4, 8 }),          // 5
            new(new[] { 5, 6, 9 }),          // 6
            new(new[] { 6, 7, 9 }),          // 7
            new(new[] { 7, 8, 9 }),          // 8
            new(new[] { 0, 1, 2, 3, 4 }),    // 9 - bottom
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new FerDeLanceModel
        {
            Name = "Fer-de-Lance",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
