using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Cargo canister from Elite (1984).
/// 10 vertices, 15 edges, 7 faces.
/// Pentagonal prism — standard cargo container.
/// </summary>
public class CanisterModel : ShipModel
{
    public static new CanisterModel Create(float size = 1.0f)
    {
        float scale = size / 24f;

        // Original Elite coordinates — 10 vertices
        var verts = new (int x, int y, int z)[]
        {
            ( 24,  16,   0),   // 0 - right face top
            ( 24,   5,  15),   // 1 - right face front right
            ( 24, -13,   9),   // 2 - right face front left
            ( 24, -13,  -9),   // 3 - right face back left
            ( 24,   5, -15),   // 4 - right face back right
            (-24,  16,   0),   // 5 - left face top
            (-24,   5,  15),   // 6 - left face front right
            (-24, -13,   9),   // 7 - left face front left
            (-24, -13,  -9),   // 8 - left face back left
            (-24,   5, -15),   // 9 - left face back right
        };

        // 15 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Front pentagon face (smaller, marked orange)
            (0, 1, Color.Orange),   // 0
            (1, 2, Color.Orange),   // 1
            (2, 3, Color.Orange),   // 2
            (3, 4, Color.Orange),   // 3
            (0, 4, Color.Orange),   // 4
            // Connecting edges
            (0, 5, null),   // 5
            (1, 6, null),   // 6
            (2, 7, null),   // 7
            (3, 8, null),   // 8
            (4, 9, null),   // 9
            // Back pentagon face (larger)
            (5, 6, null),   // 10
            (6, 7, null),   // 11
            (7, 8, null),   // 12
            (8, 9, null),   // 13
            (9, 5, null),   // 14
        };

        // 7 faces (face 2 winding corrected for outward normal)
        var faces = new Face[]
        {
            new(new[] { 0, 1, 2, 3, 4 }),      // 0 - front face
            new(new[] { 0, 5, 6, 1 }),          // 1
            new(new[] { 6, 7, 2, 1 }),          // 2 - reversed
            new(new[] { 2, 7, 8, 3 }),          // 3
            new(new[] { 3, 8, 9, 4 }),          // 4
            new(new[] { 0, 4, 9, 5 }),          // 5
            new(new[] { 5, 9, 8, 7, 6 }),      // 6 - back face
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new CanisterModel
        {
            Name = "Cargo Canister",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
