using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Sidewinder — entry-level fighter from Elite (1984).
/// 10 vertices, 15 edges, 7 faces.
/// Delta-wing design with rear engine intakes.
/// </summary>
public class SidewinderModel : ShipModel
{
    public static new SidewinderModel Create(float size = 1.0f)
    {
        float scale = size / 65f;

        // Original Elite coordinates — 10 vertices
        var verts = new (int x, int y, int z)[]
        {
            (-32,   0,  36),   // 0 - nose left
            ( 32,   0,  36),   // 1 - nose right
            ( 64,   0, -28),   // 2 - right wing tip
            (-64,   0, -28),   // 3 - left wing tip
            (  0,  16, -28),   // 4 - top rear center
            (  0, -16, -28),   // 5 - bottom rear center
            (-12,   6, -28),   // 6 - engine intake top left
            ( 12,   6, -28),   // 7 - engine intake top right
            ( 12,  -6, -28),   // 8 - engine intake bottom right
            (-12,  -6, -28),   // 9 - engine intake bottom left
        };

        // 15 edges
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (0, 1, Color.Orange),   // 0 - nose edge
            (0, 3, Color.Orange),   // 4 - left leading edge
            (1, 2, Color.Orange),   // 1 - right leading edge
            (0, 4, Color.Orange),   // 3 - nose to top rear
            (1, 4, Color.Orange),   // 2 - nose to top rear
            (0, 5, Color.Orange),   // 10 - nose to bottom rear
            (1, 5, Color.Orange),   // 9 - nose to bottom rear
            // Wing and rear edges
            (3, 4, null),   // 5
            (2, 4, null),   // 6
            (3, 5, null),   // 7
            (2, 5, null),   // 8
            // Engine intake edges (dimmer visibility)
            (6, 7, Color.Gray),   // 11
            (7, 8, Color.Gray),   // 12
            (6, 9, Color.Gray),   // 13
            (8, 9, Color.Gray),   // 14
        };

        // 7 faces
        var faces = new Face[]
        {
            new(new[] { 0, 1, 4 }),          // 0 - top front
            new(new[] { 0, 3, 4 }),          // 1 - top left
            new(new[] { 1, 2, 4 }),          // 2 - top right
            new(new[] { 3, 4, 5, 2 }),       // 3 - rear face
            new(new[] { 0, 3, 5 }),          // 4 - bottom left
            new(new[] { 0, 1, 5 }),          // 5 - bottom front
            new(new[] { 1, 2, 5 }),          // 6 - bottom right
        };

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        var edgeList = new List<Edge>();
        foreach (var (a, b, c) in edges)
            edgeList.Add(new Edge(a, b, c ?? Color.Lime));

        return new SidewinderModel
        {
            Name = "Sidewinder",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
