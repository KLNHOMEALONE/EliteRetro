using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Coriolis space station — the iconic rotating octahedron from Elite (1984).
/// 16 vertices, 28 edges, 14 faces.
/// The classic "eggbeater" station.
/// </summary>
public class CoriolisStationModel : ShipModel
{
    public static new CoriolisStationModel Create(float size = 1.0f)
    {
        float scale = size / 160f;

        // Original Elite coordinates — 16 vertices
        // Vertices 0-11: main octahedron, 12-15: front docking hatch
        var verts = new (int x, int y, int z)[]
        {
            ( 160,    0,  160),   // 0
            (   0,  160,  160),   // 1
            (-160,    0,  160),   // 2
            (   0, -160,  160),   // 3
            ( 160, -160,    0),   // 4
            ( 160,  160,    0),   // 5
            (-160,  160,    0),   // 6
            (-160, -160,    0),   // 7
            ( 160,    0, -160),   // 8
            (   0,  160, -160),   // 9
            (-160,    0, -160),   // 10
            (   0, -160, -160),   // 11
            (  10,  -30,  160),   // 12 - docking hatch
            (  10,   30,  160),   // 13
            ( -10,   30,  160),   // 14
            ( -10,  -30,  160),   // 15
        };

        // 28 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Front pyramid edges (vertices 0-7)
            (0, 3, Color.Yellow),    // 0
            (0, 1, Color.Yellow),    // 1
            (1, 2, Color.Yellow),    // 2
            (2, 3, Color.Yellow),    // 3
            (3, 4, null),            // 4
            (0, 4, null),            // 5
            (0, 5, null),            // 6
            (5, 1, null),            // 7
            (1, 6, null),            // 8
            (2, 6, null),            // 9
            (2, 7, null),            // 10
            (3, 7, null),            // 11
            // Back pyramid edges (vertices 8-11)
            (8, 11, null),           // 12
            (8, 9, null),            // 13
            (9, 10, null),           // 14
            (10, 11, null),          // 15
            // Connecting edges (front to back)
            (4, 11, null),           // 16
            (4, 8, null),            // 17
            (5, 8, null),            // 18
            (5, 9, null),            // 19
            (6, 9, null),            // 20
            (6, 10, null),           // 21
            (7, 10, null),           // 22
            (7, 11, null),           // 23
            // Docking hatch (front face rectangle)
            (12, 13, Color.Cyan),    // 24
            (13, 14, Color.Cyan),    // 25
            (14, 15, Color.Cyan),    // 26
            (15, 12, Color.Cyan),    // 27
        };

        // 14 faces for back-face culling (windings corrected for outward normals)
        var faces = new Face[]
        {
            new(new[] { 0, 1, 2, 3 }),      // 0 - front face
            new(new[] { 0, 3, 4 }),          // 1
            new(new[] { 5, 1, 0 }),          // 2 - reversed
            new(new[] { 6, 2, 1 }),          // 3 - reversed
            new(new[] { 7, 3, 2 }),          // 4 - reversed
            new(new[] { 11, 4, 3 }),         // 5 - reversed
            new(new[] { 0, 4, 5 }),          // 6
            new(new[] { 2, 6, 7 }),          // 7
            new(new[] { 1, 5, 6 }),          // 8
            new(new[] { 7, 10, 11 }),        // 9
            new(new[] { 11, 8, 4 }),         // 10 - reversed
            new(new[] { 5, 8, 9 }),          // 11
            new(new[] { 6, 9, 10 }),         // 12
            new(new[] { 11, 10, 9, 8 }),     // 13 - reversed - back face
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new CoriolisStationModel
        {
            Name = "Coriolis Station",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
