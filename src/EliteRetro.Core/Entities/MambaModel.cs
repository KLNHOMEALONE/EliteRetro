using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Mamba — large fighter from Elite (1984).
/// 25 vertices, 28 edges, 5 faces.
/// Heavy fighter with extended rear structure.
/// </summary>
public class MambaModel : ShipModel
{
    public static new MambaModel Create(float size = 1.0f)
    {
        float scale = size / 70f;

        // Original Elite coordinates — 25 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0,   0,  64),   // 0  - nose tip
            (-64,  -8, -32),   // 1  - left wing outer
            (-32,   8, -32),   // 2  - left wing inner top
            ( 32,   8, -32),   // 3  - right wing inner top
            ( 64,  -8, -32),   // 4  - right wing outer
            ( -4,   4,  16),   // 5  - canopy top left
            (  4,   4,  16),   // 6  - canopy top right
            (  8,   3,  28),   // 7  - canopy front top right
            ( -8,   3,  28),   // 8  - canopy front top left
            (-20,  -4,  16),   // 9  - hull left front
            ( 20,  -4,  16),   // 10 - hull right front
            (-24,  -7, -20),   // 11 - hull left rear outer
            (-16,  -7, -20),   // 12 - hull left rear inner
            ( 16,  -7, -20),   // 13 - hull right rear inner
            ( 24,  -7, -20),   // 14 - hull right rear outer
            ( -8,   4, -32),   // 15 - rear top left inner
            (  8,   4, -32),   // 16 - rear top right inner
            (  8,  -4, -32),   // 17 - rear bottom right inner
            ( -8,  -4, -32),   // 18 - rear bottom left inner
            (-32,   4, -32),   // 19 - left wing rear top
            ( 32,   4, -32),   // 20 - right wing rear top
            ( 36,  -4, -32),   // 21 - right wing rear bottom outer
            (-36,  -4, -32),   // 22 - left wing rear bottom outer
            (-38,   0, -32),   // 23 - left wing rear tip
            ( 38,   0, -32),   // 24 - right wing rear tip
        };

        // 28 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (0, 1, Color.Orange),   // 0  - nose to left wing
            (0, 4, Color.Orange),   // 1  - nose to right wing
            (0, 7, Color.Orange),   // nose to canopy front right
            (0, 8, Color.Orange),   // nose to canopy front left
            (0, 2, Color.Orange),   // 26 - nose to left wing inner
            (0, 3, Color.Orange),   // 27 - nose to right wing inner
            // Main hull outline
            (1, 4, null),   // 2  - wing baseline
            (1, 2, null),   // 3
            (2, 3, null),   // 4
            (3, 4, null),   // 5
            // Canopy edges (orange)
            (5, 6, Color.Orange),   // 6
            (6, 7, Color.Orange),   // 7
            (7, 8, Color.Orange),   // 8
            (5, 8, Color.Orange),   // 9
            // Lower hull edges (dimmer)
            (9, 11, Color.DarkGreen),   // 10
            (9, 12, Color.DarkGreen),   // 11
            (10, 13, Color.DarkGreen),  // 12
            (10, 14, Color.DarkGreen),  // 13
            (13, 14, Color.DarkGreen),  // 14
            (11, 12, Color.DarkGreen),  // 15
            // Rear structure edges (dimmer)
            (15, 16, Color.Gray),   // 16
            (17, 18, Color.Gray),   // 17
            (15, 18, Color.Gray),   // 18
            (16, 17, Color.Gray),   // 19
            // Wing rear edges (dimmer)
            (20, 21, Color.Gray),   // 20
            (20, 24, Color.Gray),   // 21
            (21, 24, Color.Gray),   // 22
            (19, 22, Color.Gray),   // 23
            (19, 23, Color.Gray),   // 24
            (22, 23, Color.Gray),   // 25
        };

        // 5 faces — windings corrected for outward-facing normals
        var faces = new Face[]
        {
            new(new[] { 0, 1, 4 }),          // F0 - bottom ✓
            new(new[] { 0, 3, 2 }),          // F1 - top (reversed from {0,2,3})
            new(new[] { 0, 2, 1 }),          // F2 - left (reversed from {0,1,2})
            new(new[] { 0, 4, 3 }),          // F3 - right (reversed from {0,3,4})
            new(new[] { 1, 2, 3, 4 }),       // F4 - rear ✓
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new MambaModel
        {
            Name = "Mamba",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
