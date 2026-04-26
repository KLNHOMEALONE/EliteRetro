using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Python — large luxury liner from Elite (1984).
/// 11 vertices, 26 edges, 13 faces.
/// Elongated hull with distinctive tapered rear.
/// </summary>
public class PythonModel : ShipModel
{
    public static new PythonModel Create(float size = 1.0f)
    {
        float scale = size / 224f;

        // Original Elite coordinates — 11 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0,   0, 224),   // 0  - nose tip (very long ship)
            (  0,  48,  48),   // 1  - top front
            ( 96,   0, -16),   // 2  - right wing
            (-96,   0, -16),   // 3  - left wing
            (  0,  48, -32),   // 4  - top rear upper
            (  0,  24,-112),   // 5  - top rear tip
            (-48,   0,-112),   // 6  - left rear
            ( 48,   0,-112),   // 7  - right rear
            (  0, -48,  48),   // 8  - bottom front
            (  0, -48, -32),   // 9  - bottom rear upper
            (  0, -24,-112),   // 10 - bottom rear tip
        };

        // 26 edges
        var edges = new (int a, int b, Color? c)[]
        {
            (0, 8, null),   // 0
            (0, 3, null),   // 1
            (0, 2, null),   // 2
            (0, 1, null),   // 3
            (2, 4, null),   // 4
            (1, 2, null),   // 5
            (2, 8, null),   // 6
            (1, 3, null),   // 7
            (3, 8, null),   // 8
            (2, 9, null),   // 9
            (3, 4, null),   // 10
            (3, 9, null),   // 11
            (3, 5, null),   // 12
            (3, 10, null),  // 13
            (2, 5, null),   // 14
            (2, 10, null),  // 15
            (2, 7, null),   // 16
            (3, 6, null),   // 17
            (5, 6, null),   // 18
            (5, 7, null),   // 19
            (7, 10, null),  // 20
            (6, 10, null),  // 21
            (4, 5, null),   // 22
            (9, 10, null),  // 23
            (1, 4, null),   // 24
            (8, 9, null),   // 25
        };

        // 13 faces
        var faces = new Face[]
        {
            new(new[] { 0, 1, 3 }),          // 0
            new(new[] { 0, 1, 2 }),          // 1
            new(new[] { 0, 3, 8 }),          // 2
            new(new[] { 0, 2, 8 }),          // 3
            new(new[] { 1, 3, 4 }),          // 4
            new(new[] { 1, 2, 4 }),          // 5
            new(new[] { 3, 8, 9 }),          // 6
            new(new[] { 2, 8, 9 }),          // 7
            new(new[] { 3, 4, 5, 6 }),       // 8
            new(new[] { 2, 4, 5, 7 }),       // 9
            new(new[] { 2, 9, 10, 7 }),      // 10
            new(new[] { 3, 9, 10, 6 }),      // 11
            new(new[] { 5, 6, 7 }),          // 12 - rear face
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new PythonModel
        {
            Name = "Python",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
