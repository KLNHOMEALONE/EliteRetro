using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Asteroid model from Elite (1984).
/// 9 vertices, 21 edges, 14 faces.
/// Irregular rocky body.
/// </summary>
public class AsteroidModel : ShipModel
{
    public static new AsteroidModel Create(float size = 1.0f)
    {
        float scale = size / 80f;

        // Original Elite coordinates — 9 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0,  80,   0),   // 0 - top
            ( -80, -10,   0),   // 1 - left
            (  0, -80,   0),   // 2 - bottom
            ( 70, -40,   0),   // 3 - right bottom
            ( 60,  50,   0),   // 4 - right top
            ( 50,   0,  60),   // 5 - front right
            ( -40,  0,  70),   // 6 - front left
            (  0,  30, -75),   // 7 - rear top
            (  0, -50, -60),   // 8 - rear bottom
        };

        // 21 edges
        var edges = new (int a, int b, Color? c)[]
        {
            (0, 1, null),   // 0
            (0, 4, null),   // 1
            (3, 4, null),   // 2
            (2, 3, null),   // 3
            (1, 2, null),   // 4
            (1, 6, null),   // 5
            (2, 6, null),   // 6
            (2, 5, null),   // 7
            (5, 6, null),   // 8
            (0, 5, null),   // 9
            (3, 5, null),   // 10
            (0, 6, null),   // 11
            (4, 5, null),   // 12
            (1, 8, null),   // 13
            (1, 7, null),   // 14
            (0, 7, null),   // 15
            (4, 7, null),   // 16
            (3, 7, null),   // 17
            (3, 8, null),   // 18
            (2, 8, null),   // 19
            (7, 8, null),   // 20
        };

        // 14 faces for back-face culling (windings corrected for outward normals)
        var faces = new Face[]
        {
            new(new[] { 0, 6, 5 }),          // 0 - reversed
            new(new[] { 5, 6, 2 }),          // 1 - reversed
            new(new[] { 0, 1, 6 }),          // 2
            new(new[] { 1, 2, 6 }),          // 3
            new(new[] { 2, 3, 5 }),          // 4
            new(new[] { 3, 4, 5 }),          // 5
            new(new[] { 5, 4, 0 }),          // 6 - reversed
            new(new[] { 7, 1, 0 }),          // 7 - reversed
            new(new[] { 7, 8, 1 }),          // 8 - reversed
            new(new[] { 3, 8, 7 }),          // 9
            new(new[] { 8, 2, 1 }),          // 10 - reversed
            new(new[] { 8, 3, 2 }),          // 11 - reversed
            new(new[] { 7, 4, 3 }),          // 12 - reversed
            new(new[] { 0, 4, 7 }),          // 13
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Gray));

        return new AsteroidModel
        {
            Name = "Asteroid",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
