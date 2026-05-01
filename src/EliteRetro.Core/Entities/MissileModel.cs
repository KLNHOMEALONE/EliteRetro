using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Missile model from Elite (1984).
/// 17 vertices, 24 edges, 9 faces.
/// Cone-shaped projectile with rear fins.
/// </summary>
public class MissileModel : ShipModel
{
    public static new MissileModel Create(float size = 1.0f)
    {
        float scale = size / 68f;

        // Original Elite coordinates — 17 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  68),   // 0  - nose tip
            (  8,  -8,  36),   // 1  - body front bottom right
            (  8,   8,  36),   // 2  - body front top right
            ( -8,   8,  36),   // 3  - body front top left
            ( -8,  -8,  36),   // 4  - body front bottom left
            (  8,   8, -44),   // 5  - body rear top right
            (  8,  -8, -44),   // 6  - body rear bottom right
            ( -8,  -8, -44),   // 7  - body rear bottom left
            ( -8,   8, -44),   // 8  - body rear top left
            ( 12,  12, -44),   // 9  - fin top right outer
            ( 12, -12, -44),   // 10 - fin bottom right outer
            (-12, -12, -44),   // 11 - fin bottom left outer
            (-12,  12, -44),   // 12 - fin top left outer
            ( -8,   8, -12),   // 13 - fin top left inner
            ( -8,  -8, -12),   // 14 - fin bottom left inner
            (  8,   8, -12),   // 15 - fin top right inner
            (  8,  -8, -12),   // 16 - fin bottom right inner
        };

        // 24 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose cone edges (orange)
            (0, 1, Color.Orange),   // 0
            (0, 2, Color.Orange),   // 1
            (0, 3, Color.Orange),   // 2
            (0, 4, Color.Orange),   // 3
            // Body edges
            (1, 2, null),   // 4
            (1, 4, null),   // 5
            (3, 4, null),   // 6
            (2, 3, null),   // 7
            (2, 5, null),   // 8
            (1, 6, null),   // 9
            (4, 7, null),   // 10
            (3, 8, null),   // 11
            (7, 8, null),   // 12
            (5, 8, null),   // 13
            (5, 6, null),   // 14
            (6, 7, null),   // 15
            // Fins (dimmer)
            (6, 10, Color.Gray),   // 16
            (5, 9, Color.Gray),    // 17
            (8, 12, Color.Gray),   // 18
            (7, 11, Color.Gray),   // 19
            (9, 15, Color.Gray),   // 20
            (10, 16, Color.Gray),  // 21
            (12, 13, Color.Gray),  // 22
            (11, 14, Color.Gray),  // 23
        };

        // 9 faces — windings corrected for outward-facing normals
        var faces = new Face[]
        {
            new(new[] { 0, 3, 4 }),          // F0 (reversed from {0,4,3})
            new(new[] { 0, 4, 1 }),          // F1 (reversed from {0,1,4})
            new(new[] { 0, 1, 2 }),          // F2 (reversed from {0,2,1})
            new(new[] { 0, 2, 3 }),          // F3 (reversed from {0,3,2})
            new(new[] { 1, 6, 5, 2 }),       // F4 ✓
            new(new[] { 1, 4, 7, 6 }),       // F5 ✓
            new(new[] { 3, 8, 7, 4 }),       // F6 ✓
            new(new[] { 2, 5, 8, 3 }),       // F7 ✓
            new(new[] { 5, 6, 7, 8 }),       // F8 ✓
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new MissileModel
        {
            Name = "Missile",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
