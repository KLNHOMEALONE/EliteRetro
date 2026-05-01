using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Thargon — small alien ship from Elite (1984).
/// 10 vertices, 15 edges, 7 faces.
/// Pentagonal prism shape, similar to cargo canister.
/// </summary>
public class ThargonModel : ShipModel
{
    public static new ThargonModel Create(float size = 1.0f)
    {
        float scale = size / 40f;

        // Original Elite coordinates — 10 vertices
        var verts = new (int x, int y, int z)[]
        {
            ( -9,   0,  40),   // 0 - back center top
            ( -9, -38,  12),   // 1 - back bottom right
            ( -9, -24, -32),   // 2 - back bottom left
            ( -9,  24, -32),   // 3 - back top left
            ( -9,  38,  12),   // 4 - back top right
            (  9,   0,  -8),   // 5 - front center top
            (  9, -10, -15),   // 6 - front bottom right
            (  9,  -6, -26),   // 7 - front bottom left
            (  9,   6, -26),   // 8 - front top left
            (  9,  10, -15),   // 9 - front top right
        };

        // 15 edges — 5 back, 5 front, 5 connecting
        var edges = new (int a, int b, Color? c)[]
        {
            // Front pentagon edges (orange - nose)
            (5, 6, Color.Orange),   // 5
            (6, 7, Color.Orange),   // 6
            (7, 8, Color.Orange),   // 7
            (8, 9, Color.Orange),   // 8
            (9, 5, Color.Orange),   // 9
            // Connecting edges
            (0, 5, null),   // 10
            (1, 6, null),   // 11
            (2, 7, null),   // 12
            (3, 8, null),   // 13
            (4, 9, null),   // 14
            // Back pentagon edges
            (0, 1, null),   // 0
            (1, 2, null),   // 1
            (2, 3, null),   // 2
            (3, 4, null),   // 3
            (4, 0, null),   // 4
        };

        // 7 faces — windings corrected for outward-facing normals
        var faces = new Face[]
        {
            new(new[] { 0, 4, 3, 2, 1 }),    // F0 (reversed from {0,1,2,3,4}) - back pentagon
            new(new[] { 0, 1, 6, 5 }),       // F1 ✓
            new(new[] { 1, 2, 7, 6 }),       // F2 ✓
            new(new[] { 2, 3, 8, 7 }),       // F3 ✓
            new(new[] { 3, 4, 9, 8 }),       // F4 ✓
            new(new[] { 4, 0, 5, 9 }),       // F5 ✓
            new(new[] { 5, 6, 7, 8, 9 }),    // F6 - front pentagon ✓
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new ThargonModel
        {
            Name = "Thargon",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
