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

        // 27 edges (added (6,7) for rear face boundary)
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (0, 1, Color.Orange),   // 3
            (0, 2, Color.Orange),   // 2
            (0, 3, Color.Orange),   // 1
            (0, 8, Color.Orange),   // 0
            // Rest of hull
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
            (6, 7, null),   // 26 - rear face edge
        };

        // 13 faces — windings corrected for outward-facing normals
        var faces = new Face[]
        {
            new(new[] { 0, 1, 3 }),          // F0 - top left front ✓
            new(new[] { 0, 2, 1 }),          // F1 - top right front (reversed)
            new(new[] { 0, 3, 8 }),          // F2 - bottom left front ✓
            new(new[] { 0, 8, 2 }),          // F3 - bottom right front (reversed)
            new(new[] { 1, 4, 3 }),          // F4 - top left rear (reversed)
            new(new[] { 1, 2, 4 }),          // F5 - top right rear ✓
            new(new[] { 3, 9, 8 }),          // F6 - bottom left rear (reversed)
            new(new[] { 2, 8, 9 }),          // F7 - bottom right rear ✓
            new(new[] { 3, 4, 5, 6 }),       // F8 - left side ✓
            new(new[] { 2, 7, 5, 4 }),       // F9 - right side (reversed)
            new(new[] { 2, 9, 10, 7 }),      // F10 - bottom right rear ✓
            new(new[] { 3, 6, 10, 9 }),      // F11 - bottom left rear (reversed)
            new(new[] { 5, 7, 6 }),          // F12 - rear face (reversed)
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
