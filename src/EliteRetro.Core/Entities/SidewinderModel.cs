using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Sidewinder — entry-level fighter from Elite (1984).
/// 10 vertices, 17 edges, 7 faces.
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

        // 15 edges (13 from face boundaries + 2 engine intake edges)
        var edges = new (int a, int b, Color? c)[]
        {
            // Nose edges (orange)
            (0, 1, Color.Orange),   // 0 - nose edge
            (0, 3, Color.Orange),   // 1 - left leading edge
            (1, 2, Color.Orange),   // 2 - right leading edge
            (0, 4, Color.Orange),   // 3 - nose left to top rear
            (1, 4, Color.Orange),   // 4 - nose right to top rear
            (0, 5, Color.Orange),   // 5 - nose left to bottom rear
            (1, 5, Color.Orange),   // 6 - nose right to bottom rear
            // Wing and rear edges
            (3, 4, null),   // 7
            (2, 4, null),   // 8
            (3, 5, null),   // 9
            (2, 5, null),   // 10
            // Missing edges from face boundary analysis
            (2, 3, null),   // 11 - right wing to left wing (from F3 rear face)
            (4, 5, null),   // 12 - top rear to bottom rear (from F3 rear face)
            // Engine intake edges (dimmer visibility)
            (6, 7, Color.Gray),   // 13
            (7, 8, Color.Gray),   // 14
            (6, 9, Color.Gray),   // 15
            (8, 9, Color.Gray),   // 16
        };

        // 7 faces with corrected windings (normals point outward from origin)
        var faces = new Face[]
        {
            new(new[] { 0, 1, 4 }),          // 0 - top front
            new(new[] { 4, 3, 0 }),          // 1 - top left (reversed from {0,3,4})
            new(new[] { 1, 2, 4 }),          // 2 - top right
            new(new[] { 3, 4, 5, 2 }),       // 3 - rear face
            new(new[] { 0, 3, 5 }),          // 4 - bottom left
            new(new[] { 5, 1, 0 }),          // 5 - bottom front (reversed from {0,1,5})
            new(new[] { 5, 2, 1 }),          // 6 - bottom right (reversed from {1,2,5})
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
