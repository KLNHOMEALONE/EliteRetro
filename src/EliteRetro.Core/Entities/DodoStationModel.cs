using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Dodo Station — large space station from Elite enhanced versions.
/// 24 vertices, 34 edges, 12 faces.
/// Massive orbital station, larger than the Coriolis.
/// </summary>
public class DodoStationModel : ShipModel
{
    public static new DodoStationModel Create(float size = 1.0f)
    {
        float scale = size / 250f;

        var verts = new (int x, int y, int z)[]
        {
            // Main octahedron frame
            (231,   0,   0),   // 0 - +X
            (-231,  0,   0),   // 1 - -X
            (0,   243,   0),   // 2 - +Y
            (0,  -243,   0),   // 3 - -Y
            (0,     0, 196),   // 4 - +Z (front)
            (0,     0,-196),   // 5 - -Z (rear)
            // Docking ring vertices
            (150, 150,   0),   // 6
            (-150, 150,  0),   // 7
            (-150,-150,  0),   // 8
            (150,-150,   0),   // 9
            // Front face details
            (80,   80, 100),   // 10
            (-80,  80, 100),   // 11
            (-80, -80, 100),   // 12
            (80,  -80, 100),   // 13
            // Rear face details
            (80,   80,-100),   // 14
            (-80,  80,-100),   // 15
            (-80, -80,-100),   // 16
            (80,  -80,-100),   // 17
            // Docking port
            (50,   50, 150),   // 18
            (-50,  50, 150),   // 19
            (-50, -50, 150),   // 20
            (50,  -50, 150),   // 21
            // Center hub
            (0,    0, 180),    // 22 - docking port center
            (0,    0, -150),   // 23 - rear center
        };

        var edges = new (int a, int b, Color? c)[]
        {
            // Main octahedron
            (0, 2, Color.Green),
            (0, 3, Color.Green),
            (0, 4, Color.Green),
            (0, 5, Color.Green),
            (1, 2, Color.Green),
            (1, 3, Color.Green),
            (1, 4, Color.Green),
            (1, 5, Color.Green),
            (2, 4, Color.Green),
            (2, 5, Color.Green),
            (3, 4, Color.Green),
            (3, 5, Color.Green),
            // Docking ring
            (6, 7, Color.Cyan),
            (7, 8, Color.Cyan),
            (8, 9, Color.Cyan),
            (9, 6, Color.Cyan),
            // Front details
            (10, 11, Color.Yellow),
            (11, 12, Color.Yellow),
            (12, 13, Color.Yellow),
            (13, 10, Color.Yellow),
            // Rear details
            (14, 15, Color.Yellow),
            (15, 16, Color.Yellow),
            (16, 17, Color.Yellow),
            (17, 14, Color.Yellow),
            // Docking port
            (18, 19, Color.Cyan),
            (19, 20, Color.Cyan),
            (20, 21, Color.Cyan),
            (21, 18, Color.Cyan),
            (18, 22, Color.Cyan),
            (19, 22, Color.Cyan),
            (20, 22, Color.Cyan),
            (21, 22, Color.Cyan),
            (23, 14, Color.Green),
            (23, 15, Color.Green),
        };

        var faces = new Face[]
        {
            new(new[] { 0, 2, 4 }),
            new(new[] { 0, 4, 3 }),
            new(new[] { 0, 3, 5 }),
            new(new[] { 0, 5, 2 }),
            new(new[] { 1, 2, 5 }),
            new(new[] { 1, 5, 3 }),
            new(new[] { 1, 3, 4 }),
            new(new[] { 1, 4, 2 }),
            new(new[] { 10, 11, 12, 13 }),
            new(new[] { 14, 15, 16, 17 }),
            new(new[] { 18, 19, 20, 21 }),
            new(new[] { 6, 7, 8, 9 }),
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Green));

        return new DodoStationModel
        {
            Name = "Dodo Station",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
