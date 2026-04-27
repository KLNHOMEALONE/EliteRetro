using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Cobra Mk3 — the iconic ship from Elite (1984).
/// Authentic wireframe model from the original BBC Micro data.
/// 28 vertices, 38 edges.
/// </summary>
public class CobraMk3Model : ShipModel
{
    public static new CobraMk3Model Create(float size = 1.0f)
    {
        float scale = size / 120f;

        // Original Elite coordinates — 28 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  32,   0,  76),   // 0  - nose right
            ( -32,   0,  76),   // 1  - nose left
            (   0,  26,  24),   // 2  - canopy top
            (-120,  -3,  -8),   // 3  - left pod outer
            ( 120,  -3,  -8),   // 4  - right pod outer
            ( -88,  16, -40),   // 5  - left wing tip top
            (  88,  16, -40),   // 6  - right wing tip top
            ( 128,  -8, -40),   // 7  - right pod bottom outer
            (-128,  -8, -40),   // 8  - left pod bottom outer
            (   0,  26, -40),   // 9  - rear top center
            ( -32, -24, -40),   // 10 - rear bottom left
            (  32, -24, -40),   // 11 - rear bottom right
            ( -36,   8, -40),   // 12 - rear left inner
            (  -8,  12, -40),   // 13 - rear top left inner
            (   8,  12, -40),   // 14 - rear top right inner
            (  36,   8, -40),   // 15 - rear right inner
            (  36, -12, -40),   // 16 - rear bottom right inner
            (  -8, -16, -40),   // 17 - rear bottom left inner
            (   8, -16, -40),   // 18 - rear bottom right inner
            ( -36, -12, -40),   // 19 - rear bottom left inner
            (   0,   0,  76),   // 20 - nose center
            (   0,   0,  90),   // 21 - nose tip (laser mount)
            ( -80,  -6, -40),   // 22 - left pod inner
            ( -80,   6, -40),   // 23 - left pod inner top
            ( -88,   0, -40),   // 24 - left pod tip
            (  80,   6, -40),   // 25 - right pod inner top
            (  88,   0, -40),   // 26 - right pod tip
            (  80,  -6, -40),   // 27 - right pod inner
        };

        // 38 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (0, 1, Color.Orange), (0, 2, Color.Orange), (0, 4, Color.Orange),
            (1, 2, Color.Orange), (1, 3, Color.Orange), (20, 21, Color.Orange),
            // Rest of hull
            (3, 8, null), (4, 7, null), (6, 7, null), (6, 9, null),
            (5, 9, null), (5, 8, null), (2, 5, null), (2, 6, null),
            (3, 5, null), (4, 6, null), (8, 10, null),
            (10, 11, null), (7, 11, null), (1, 10, null), (0, 11, null),
            (1, 5, Color.Gray), (0, 6, Color.Gray),
            (12, 13, null), (18, 19, null), (14, 15, null), (16, 17, null),
            (15, 16, null), (14, 17, null), (13, 18, null), (12, 19, null),
            (13, 14, null), (12, 10, null), (15, 11, null), (17, 10, null),
            (18, 11, null), (9, 13, null), (9, 14, null),
        };

        // 13 faces for back-face culling - authentic data from cobramk3.txt
        var faces = new Face[]
        {
            new(new[] { 0, 1, 2 }, new Vector3(0, 62, 31)),           // Face 0
            new(new[] { 1, 2, 5 }, new Vector3(-18, 55, 16)),         // Face 1
            new(new[] { 0, 2, 6 }, new Vector3(18, 55, 16)),          // Face 2
            new(new[] { 1, 3, 5 }, new Vector3(-16, 52, 14)),         // Face 3
            new(new[] { 0, 4, 6 }, new Vector3(16, 52, 14)),          // Face 4
            new(new[] { 2, 5, 9, 6 }, new Vector3(-14, 47, 0)),       // Face 5 (merged 5 & 6)
            new(new[] { 3, 8, 5 }, new Vector3(-61, 102, 0)),         // Face 7
            new(new[] { 4, 7, 6 }, new Vector3(61, 102, 0)),          // Face 8
            new(new[] { 9, 6, 7, 11, 10, 8, 5 }, new Vector3(0, 0, -80)), // Face 9
            new(new[] { 1, 3, 8, 10 }, new Vector3(-7, -42, 9)),      // Face 10
            new(new[] { 0, 1, 10, 11 }, new Vector3(0, -30, 6)),      // Face 11
            new(new[] { 0, 4, 7, 11 }, new Vector3(7, -42, 9)),       // Face 12
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new CobraMk3Model
        {
            Name = "Cobra Mk3",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
