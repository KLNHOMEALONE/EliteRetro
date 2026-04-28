using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Planet wireframe model — icosahedron-based sphere with craters and meridians.
/// Used for planet rendering in the flight scene.
/// </summary>
public class PlanetModel : ShipModel
{
    /// <summary>
    /// Create a planet model with given radius.
    /// Generates an icosahedron base with subdivided faces for a spherical shape.
    /// </summary>
    public static PlanetModel Create(float radius = GameConstants.PlanetRadius)
    {
        float a = radius * 0.525731f; // icosahedron scale factor
        float b = radius * 0.850651f;

        // 12 vertices of an icosahedron
        var verts = new (int x, int y, int z)[]
        {
            (0, (int)(a * 1000), (int)(-b * 1000)),   // 0
            ((int)(b * 1000), 0, (int)(a * 1000)),     // 1
            ((int)(-b * 1000), 0, (int)(a * 1000)),    // 2
            (0, (int)(a * 1000), (int)(b * 1000)),     // 3
            ((int)(-a * 1000), (int)(b * 1000), 0),    // 4
            ((int)(a * 1000), (int)(b * 1000), 0),     // 5
            ((int)(a * 1000), (int)(-b * 1000), 0),    // 6
            (0, (int)(-a * 1000), (int)(b * 1000)),    // 7
            (0, (int)(-a * 1000), (int)(-b * 1000)),   // 8
            ((int)(-a * 1000), (int)(-b * 1000), 0),   // 9
            ((int)(-b * 1000), 0, (int)(-a * 1000)),   // 10
            ((int)(b * 1000), 0, (int)(-a * 1000)),    // 11
        };

        float scale = radius / 1000f;

        var vertices = new List<Vertex3>();
        foreach (var (x, y, z) in verts)
            vertices.Add(new Vertex3(x * scale, y * scale, z * scale));

        // 30 edges of an icosahedron
        var edges = new (int a, int b, Color? c)[]
        {
            // Upper cap
            (0, 1, null), (0, 2, null), (0, 4, null), (0, 5, null), (1, 5, null), (2, 4, null),
            // Middle band
            (1, 6, null), (1, 11, null), (2, 9, null), (2, 10, null), (4, 10, null), (4, 7, null),
            (5, 6, null), (5, 11, null), (9, 10, null), (7, 10, null),
            // Lower cap
            (6, 8, null), (6, 11, null), (8, 9, null), (8, 11, null), (3, 7, null), (3, 1, null),
            (3, 5, null), (3, 7, null), (7, 8, null), (9, 8, null),
        };

        // 20 triangular faces of an icosahedron
        var faces = new Face[]
        {
            new(new[] { 0, 1, 5 }),
            new(new[] { 0, 5, 4 }),
            new(new[] { 0, 4, 2 }),
            new(new[] { 0, 2, 1 }),
            new(new[] { 1, 2, 10 }),
            new(new[] { 1, 10, 11 }),
            new(new[] { 5, 1, 11 }),
            new(new[] { 5, 11, 6 }),
            new(new[] { 4, 5, 6 }),
            new(new[] { 4, 6, 9 }),
            new(new[] { 2, 4, 9 }),
            new(new[] { 2, 9, 10 }),
            new(new[] { 3, 7, 10 }),
            new(new[] { 3, 10, 2 }),
            new(new[] { 3, 2, 0 }),
            new(new[] { 3, 0, 1 }),
            new(new[] { 3, 1, 5 }),
            new(new[] { 3, 5, 4 }),
            new(new[] { 3, 4, 7 }),
            new(new[] { 3, 7, 8 }),
        };

        var edgeList = new List<Edge>();
        foreach (var (aa, bb, c) in edges)
            edgeList.Add(new Edge(aa, bb, c ?? Color.Lime));

        return new PlanetModel
        {
            Name = "Planet",
            Vertices = vertices,
            Edges = edgeList,
            Faces = faces.ToList(),
        };
    }
}
