using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Defines a 3D ship model as vertices and edges for wireframe rendering.
/// </summary>
public record struct Vertex3(float X, float Y, float Z);

/// <summary>
/// An edge connecting two vertex indices with an optional color.
/// </summary>
public record struct Edge(int Start, int End, Color? Color = null);

    /// <summary>
    /// A face (polygon) defined by vertex indices and an optional pre-computed normal vector.
    /// If the normal is not provided (default Vector3.Zero), it will be computed at runtime
    /// using Newell's method. When provided (as in original Elite blueprints), it is used directly.
    /// </summary>
    public record struct Face(int[] VertexIndices, Vector3? Normal = null);

    /// <summary>
    /// Complete ship model definition.
    /// </summary>
    public class ShipModel
    {
        public string Name { get; init; } = "";
        public List<Vertex3> Vertices { get; init; } = new();
        public List<Edge> Edges { get; init; } = new();
        public List<Face> Faces { get; init; } = new();

        /// <summary>True for non-ship entities: asteroids, boulders, rock hermits.</summary>
        public bool IsRock { get; init; }

        // NE-12 optimization: Cached edge-to-face adjacency for fast silhouette computation.
        // Precomputed once per model (static geometry) to avoid O(E×V×F) allocations per frame.
        // Each entry lists face indices that reference this edge.
        private List<int>[]? _edgeFacesCache;

        /// <summary>
        /// Get or build the edge-to-face adjacency map for this model.
        /// Result is cached on first use.
        /// </summary>
        public List<int>[] GetEdgeFaces()
        {
            if (_edgeFacesCache != null)
                return _edgeFacesCache;

            _edgeFacesCache = new List<int>[Edges.Count];
            for (int e = 0; e < Edges.Count; e++)
                _edgeFacesCache[e] = new List<int>();

            if (Faces.Count > 0)
            {
                // Build a vertex set per face for fast lookup
                var faceVertexSets = new HashSet<int>[Faces.Count];
                for (int f = 0; f < Faces.Count; f++)
                    faceVertexSets[f] = new HashSet<int>(Faces[f].VertexIndices);

                // For each edge, find faces containing both vertices
                for (int e = 0; e < Edges.Count; e++)
                {
                    int start = Edges[e].Start;
                    int end = Edges[e].End;
                    for (int f = 0; f < Faces.Count; f++)
                    {
                        if (faceVertexSets[f].Contains(start) && faceVertexSets[f].Contains(end))
                            _edgeFacesCache[e].Add(f);
                    }
                }
            }

            return _edgeFacesCache;
        }

        /// <summary>
        /// Pre-built vertex sets for each face to avoid repeated HashSet allocations.
        /// Cached on first access.
        /// </summary>
        private HashSet<int>[]? _faceVertexSetsCache;

        /// <summary>
        /// Get the pre-computed vertex index sets for all faces.
        /// </summary>
        public HashSet<int>[] GetFaceVertexSets()
        {
            if (_faceVertexSetsCache != null)
                return _faceVertexSetsCache;

            _faceVertexSetsCache = new HashSet<int>[Faces.Count];
            for (int f = 0; f < Faces.Count; f++)
                _faceVertexSetsCache[f] = new HashSet<int>(Faces[f].VertexIndices);

            return _faceVertexSetsCache;
        }


    /// <summary>
    /// Create a simple cube model for testing.
    /// </summary>
    public static ShipModel CreateCube(float size = 1.0f)
    {
        float s = size / 2;
        return new ShipModel
        {
            Name = "Cube",
            Vertices = new List<Vertex3>
            {
                new(-s, -s, -s), new(s, -s, -s), new(s, s, -s), new(-s, s, -s),
                new(-s, -s, s), new(s, -s, s), new(s, s, s), new(-s, s, s)
            },
            Edges = new List<Edge>
            {
                // Back face
                new(0, 1), new(1, 2), new(2, 3), new(3, 0),
                // Front face
                new(4, 5), new(5, 6), new(6, 7), new(7, 4),
                // Connecting edges
                new(0, 4), new(1, 5), new(2, 6), new(3, 7)
            },
            Faces = new List<Face>
            {
                new(new[] { 0, 1, 2, 3 }), // Back
                new(new[] { 4, 5, 6, 7 }), // Front
                new(new[] { 0, 1, 5, 4 }), // Bottom
                new(new[] { 2, 3, 7, 6 }), // Top
                new(new[] { 0, 3, 7, 4 }), // Left
                new(new[] { 1, 2, 6, 5 })  // Right
            }
        };
    }
}
