using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Escape pod from Elite (1984).
/// 4 vertices, 6 edges, 4 faces.
/// Small tetrahedron shape.
/// </summary>
public class EscapePodModel : ShipModel
{
    public static new EscapePodModel Create(float size = 1.0f)
    {
        float scale = size / 36f;

        // Original Elite coordinates — 4 vertices (tetrahedron)
        var verts = new (int x, int y, int z)[]
        {
            ( -7,   0,  36),   // 0 - back top
            ( -7, -14, -12),   // 1 - back bottom right
            ( -7,  14, -12),   // 2 - back bottom left
            ( 21,   0,   0),   // 3 - front tip
        };

        // 6 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (3, 0, Color.Orange),   // 3 - front tip to back top
            (3, 1, Color.Orange),   // 5 - front tip to back bottom right
            (3, 2, Color.Orange),   // 2 - front tip to back bottom left
            // Back edges
            (0, 1, null),   // 0
            (1, 2, null),   // 1
            (0, 2, null),   // 4
        };

        // 4 faces (tetrahedron)
        var faces = new Face[]
        {
            new(new[] { 1, 2, 3 }),   // 0
            new(new[] { 0, 2, 3 }),   // 1
            new(new[] { 0, 1, 3 }),   // 2
            new(new[] { 0, 1, 2 }),   // 3 - back face
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Yellow));

        return new EscapePodModel
        {
            Name = "Escape Pod",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
