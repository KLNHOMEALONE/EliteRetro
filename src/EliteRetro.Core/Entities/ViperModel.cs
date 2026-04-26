using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Viper — police ship from Elite (1984).
/// 15 vertices, 20 edges, 7 faces.
/// Sleek design with distinctive rear engine assembly.
/// </summary>
public class ViperModel : ShipModel
{
    public static new ViperModel Create(float size = 1.0f)
    {
        float scale = size / 75f;

        // Original Elite coordinates — 15 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  72),   // 0  - nose tip
            (  0,  16,  24),   // 1  - top front
            (  0, -16,  24),   // 2  - bottom front
            ( 48,   0, -24),   // 3  - right wing tip
            (-48,   0, -24),   // 4  - left wing tip
            ( 24, -16, -24),   // 5  - right bottom rear
            (-24, -16, -24),   // 6  - left bottom rear
            ( 24,  16, -24),   // 7  - right top rear
            (-24,  16, -24),   // 8  - left top rear
            (-32,   0, -24),   // 9  - left engine outer
            ( 32,   0, -24),   // 10 - right engine outer
            (  8,   8, -24),   // 11 - right engine top inner
            ( -8,   8, -24),   // 12 - left engine top inner
            ( -8,  -8, -24),   // 13 - left engine bottom inner
            (  8,  -8, -24),   // 14 - right engine bottom inner
        };

        // 20 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Main hull edges
            (0, 3, null),   // 0  - nose to right wing
            (0, 1, Color.Orange),   // 1
            (0, 2, Color.Orange),   // 2
            (0, 4, null),   // 3  - nose to left wing
            (1, 7, Color.Orange),   // 4
            (1, 8, Color.Orange),   // 5
            (2, 5, Color.Orange),   // 6
            (2, 6, Color.Orange),   // 7
            (7, 8, null),   // 8  - top rear edge
            (5, 6, null),   // 9  - bottom rear edge
            (4, 8, null),   // 10 - left wing to top
            (4, 6, null),   // 11 - left wing to bottom
            (3, 7, null),   // 12 - right wing to top
            (3, 5, null),   // 13 - right wing to bottom
            // Engine assembly edges (dimmer)
            (9, 12, Color.Gray),   // 14
            (9, 13, Color.Gray),   // 15
            (10, 11, Color.Gray),  // 16
            (10, 14, Color.Gray),  // 17
            (11, 14, Color.Gray),  // 18
            (12, 13, Color.Gray),  // 19
        };

        // 7 faces
        var faces = new Face[]
        {
            new(new[] { 1, 7, 8 }),          // 0 - top
            new(new[] { 0, 4, 1 }),          // 1 - top left front
            new(new[] { 0, 3, 1 }),          // 2 - top right front
            new(new[] { 0, 4, 2 }),          // 3 - bottom left front
            new(new[] { 0, 2, 3 }),          // 4 - bottom right front
            new(new[] { 2, 6, 5 }),          // 5 - bottom rear
            new(new[] { 3, 7, 8, 4, 6, 5 }), // 6 - rear face (hexagon with engine cutout)
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new ViperModel
        {
            Name = "Viper",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
