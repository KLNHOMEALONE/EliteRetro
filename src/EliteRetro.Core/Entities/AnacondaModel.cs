using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Anaconda — largest ship in Elite (1984).
/// 15 vertices, 24 edges, 12 faces.
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

        // 24 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (7, 12, Color.Orange),
            (8, 12, Color.Orange),
            (11, 12, Color.Orange),
            (12, 13, Color.Orange),
            (12, 14, Color.Orange),
            (10, 12, Color.Orange),
            // Hull edges
            (0, 1, null), (1, 2, null), (2, 3, null), (3, 4, null), (0, 4, null),
            (0, 5, null), (1, 6, null), (2, 7, null), (3, 8, null), (4, 9, null),
            (5, 10, null), (6, 10, null), (6, 11, null), (7, 11, null),
            (9, 13, null), (9, 14, null), (5, 14, null), (10, 14, null),
        };

        // 12 faces with verified outward-pointing normals.
        // F5 winding corrected from {0,4,9,5} to {0,5,9,4}.
        // Normals computed via cross product, verified outward by dot(normal, centroid) > 0.
        var faces = new Face[]
        {
            new(new[] { 0, 1, 2, 3, 4 }, new Vector3( -34, -1819, -1802)),   // F0: rear bottom
            new(new[] { 0, 1, 6, 5 },    new Vector3(-1028,   400, -1724)),   // F1: left rear
            new(new[] { 1, 2, 7, 6 },    new Vector3(-1734, -1309,  -442)),   // F2: left side
            new(new[] { 2, 3, 8, 7 },    new Vector3(    0, -2236,   416)),   // F3: bottom front
            new(new[] { 3, 4, 9, 8 },    new Vector3( 1700, -1258,  -408)),   // F4: right side
            new(new[] { 0, 5, 9, 4 },    new Vector3( 1028,   400, -1724)),   // F5: right rear (winding fixed)
            new(new[] { 5, 10, 14 },     new Vector3(    0,  2236,  -430)),   // F6: top rear
            new(new[] { 6, 11, 10 },     new Vector3(-1658,  1222,   416)),   // F7: left top (winding fixed)
            new(new[] { 7, 12, 11 },     new Vector3(-8444, -5220,  2648)),   // F8: left front (winding fixed)
            new(new[] { 8, 13, 12 },     new Vector3( 8444, -5220,  2648)),   // F9: right front (winding fixed)
            new(new[] { 9, 14, 13 },     new Vector3( 1658,  1222,   416)),   // F10: right top (winding fixed)
            new(new[] { 10, 12, 14 },    new Vector3(    0, 23822,  4558)),   // F11: top front
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
