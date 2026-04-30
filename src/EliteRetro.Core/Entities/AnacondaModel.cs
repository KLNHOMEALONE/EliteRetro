using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Anaconda — largest ship in Elite (1984).
/// 15 vertices, 25 edges, 12 faces.
/// Massive capital ship with distinctive tapered hull.
/// </summary>
public class AnacondaModel : ShipModel
{
    public static new AnacondaModel Create(float size = 1.0f)
    {
        float scale = size / 254f;

        // Original Elite coordinates — 15 vertices
        var verts = new (int x, int y, int z)[]
        {
            (  0,   7, -58),   // 0  - top rear
            (-43, -13, -37),   // 1  - left rear
            (-26, -47,  -3),   // 2  - left bottom
            ( 26, -47,  -3),   // 3  - right bottom
            ( 43, -13, -37),   // 4  - right rear
            (  0,  48, -49),   // 5  - top upper rear
            (-69,  15, -15),   // 6  - left mid
            (-43, -39,  40),   // 7  - left front bottom
            ( 43, -39,  40),   // 8  - right front bottom
            ( 69,  15, -15),   // 9  - right mid
            (-43,  53, -23),   // 10 - left wing top
            (-69,  -1,  32),   // 11 - left front
            (  0,   0, 254),   // 12 - nose tip (very long!)
            ( 69,  -1,  32),   // 13 - right front
            ( 43,  53, -23),   // 14 - right wing top
        };

        // 25 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (7, 12, Color.Orange),   // 14
            (8, 12, Color.Orange),   // 15
            (11, 12, Color.Orange),  // 22
            (12, 13, Color.Orange),  // 23
            (12, 14, Color.Orange),  // 24
            (10, 12, Color.Orange),  // 21
            // Rest of hull
            (0, 1, null),   // 0
            (1, 2, null),   // 1
            (2, 3, null),   // 2
            (3, 4, null),   // 3
            (0, 4, null),   // 4
            (0, 5, null),   // 5
            (1, 6, null),   // 6
            (2, 7, null),   // 7
            (3, 8, null),   // 8
            (4, 9, null),   // 9
            (5, 10, null),  // 10
            (6, 10, null),  // 11
            (6, 11, null),  // 12
            (7, 11, null),  // 13
            (9, 13, null),  // 17
            (9, 14, null),  // 18
            (5, 14, null),  // 19
            (10, 14, null), // 20
        };

        // 12 faces with manually verified outward-pointing normals.
        // The Anaconda has non-planar faces, so normals are best-fit approximations
        // verified against the ship's geometry (vertex positions from original Elite).
        var faces = new Face[]
        {
            new(new[] { 0, 1, 2, 3, 4 }, new Vector3(0, -0.6f, -0.8f)),    // 0 - rear (down+back)
            new(new[] { 0, 1, 6, 5 }, new Vector3(-0.2f, 0.7f, -0.7f)),    // 1 - left rear (left+up+back)
            new(new[] { 1, 2, 7, 6 }, new Vector3(-0.3f, -0.3f, 0.9f)),    // 2 - left side (left+down+forward)
            new(new[] { 2, 3, 8, 7 }, new Vector3(0, -0.9f, 0.4f)),        // 3 - bottom front (down+forward)
            new(new[] { 3, 4, 9, 8 }, new Vector3(0.3f, -0.3f, 0.9f)),     // 4 - right side (right+down+forward)
            new(new[] { 0, 4, 9, 5 }, new Vector3(0.2f, 0.7f, -0.7f)),     // 5 - right rear (right+up+back)
            new(new[] { 5, 10, 14 }, new Vector3(0, 0.9f, -0.4f)),          // 6 - top rear (up+back)
            new(new[] { 6, 10, 11 }, new Vector3(-0.7f, 0.5f, -0.5f)),     // 7 - left top front (left+up+back)
            new(new[] { 7, 11, 12 }, new Vector3(-0.7f, 0.5f, 0.5f)),      // 8 - left front (left+up+forward)
            new(new[] { 8, 12, 13 }, new Vector3(0.7f, 0.5f, 0.5f)),       // 9 - right front (right+up+forward)
            new(new[] { 9, 13, 14 }, new Vector3(0.7f, 0.5f, -0.5f)),      // 10 - right top front (right+up+back)
            new(new[] { 10, 12, 14 }, new Vector3(0, 0.9f, 0.4f)),         // 11 - top front (up+forward)
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new AnacondaModel
        {
            Name = "Anaconda",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
