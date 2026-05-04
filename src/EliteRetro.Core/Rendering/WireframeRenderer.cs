using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Renders 3D ship models as wireframes with back-face culling.
/// Visible edges are solid; hidden edges are drawn dashed.
/// </summary>
public class WireframeRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Color _primaryColor = Color.Lime;
    private Texture2D _lineTexture = null!;

    // Dash pattern for hidden edges: dash length, gap length (in pixels)
    private const float DashLength = 8f;
    private const float GapLength = 4f;

    public WireframeRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _lineTexture = new Texture2D(graphicsDevice, 1, 1);
        _lineTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Project a 3D vertex to 2D screen coordinates.
    /// </summary>
    public Vector2 Project(Vertex3 vertex, Matrix world, Matrix view, Matrix projection)
    {
        var viewport = _graphicsDevice.Viewport;
        Vector3 v3 = new Vector3(vertex.X, vertex.Y, vertex.Z);

        // Transform by world*view*projection
        Matrix combined = world * view * projection;
        Vector3 transformed = Vector3.Transform(v3, combined);

        // Perspective divide
        if (Math.Abs(transformed.Z) < 0.001f)
            return new Vector2(float.NaN, float.NaN);

        float ndcX = transformed.X / transformed.Z;
        float ndcY = transformed.Y / transformed.Z;

        return new Vector2(
            (ndcX + 1) / 2 * viewport.Width + viewport.X,
            (1 - ndcY) / 2 * viewport.Height + viewport.Y
        );
    }

    /// <summary>
    /// Check if a face is facing the camera.
    /// Follows the optimized algorithm from docs\shiprendering\Back-face-Culling.md
    /// </summary>
    private bool IsFaceVisible(Face face, ShipModel model, Vector3 shipToCameraLocal, Vector3[]? worldVerts, Vector3 cameraPosition, Matrix world)
    {
        if (face.VertexIndices.Length < 3) return false;

        // AUTHENTIC ELITE CULLING (if normal is provided in blueprint)
        if (face.Normal.HasValue)
        {
            // Face is visible when the camera is on the side the normal points to.
            // Dot(shipToCamera, normal) > 0 means the camera is in the hemisphere the normal faces.
            return Vector3.Dot(shipToCameraLocal, face.Normal.Value) > 0;
        }

        // FALLBACK: NEWELL'S METHOD (for models without pre-computed normals)
        if (worldVerts == null) return false;

        int n = face.VertexIndices.Length;
        Vector3 normal = Vector3.Zero;
        Vector3 faceCenter = Vector3.Zero;

        for (int i = 0; i < n; i++)
        {
            Vector3 current = worldVerts[face.VertexIndices[i]];
            Vector3 next = worldVerts[face.VertexIndices[(i + 1) % n]];
            normal.X += (current.Y - next.Y) * (current.Z + next.Z);
            normal.Y += (current.Z - next.Z) * (current.X + next.X);
            normal.Z += (current.X - next.X) * (current.Y + next.Y);
            faceCenter += current;
        }
        faceCenter /= n;

        // Ensure normal points outward from ship center
        Vector3 shipCenter = world.Translation;
        if (Vector3.Dot(normal, faceCenter - shipCenter) < 0)
            normal = -normal;

        Vector3 lineOfSight = cameraPosition - faceCenter;
        return Vector3.Dot(normal, lineOfSight) > 0;
    }

    /// <summary>
    /// Draw a ship wireframe. Visible edges are solid, hidden edges are dashed.
    /// Optionally highlight a specific edge in red.
    /// </summary>
    public void Draw(ShipModel model, Matrix world, Matrix view, Matrix projection, SpriteBatch spriteBatch, bool useBackFaceCulling = true, int highlightedEdgeIndex = -1, bool drawHiddenEdges = true, bool drawWhite = false)
    {
        // Project all vertices
        var projected = new Vector2[model.Vertices.Count];
        for (int i = 0; i < model.Vertices.Count; i++)
        {
            projected[i] = Project(model.Vertices[i], world, view, projection);
        }

        // Determine edge visibility via back-face culling
        var edgeVisible = new bool[model.Edges.Count];
        var edgeHasFace = new bool[model.Edges.Count];

        if (useBackFaceCulling && model.Faces.Count > 0)
        {
            // Pre-calculate data needed for culling
            Matrix invView = Matrix.Invert(view);
            Vector3 cameraPosition = invView.Translation;
            Vector3 shipPosition = world.Translation;

            // Vector from ship to camera (line of sight from ship's perspective)
            Vector3 shipToCamera = cameraPosition - shipPosition;

            // Transform to ship-local space (Elite's "Projected Line of Sight")
            Matrix worldToLocal = Matrix.Invert(world);
            Vector3 shipToCameraLocal = Vector3.TransformNormal(shipToCamera, worldToLocal);

            // Pre-calculate world vertices only if we have faces that need Newell's method
            Vector3[]? worldVerts = null;
            if (model.Faces.Any(f => !f.Normal.HasValue))
            {
                worldVerts = new Vector3[model.Vertices.Count];
                for (int i = 0; i < model.Vertices.Count; i++)
                {
                    var v = model.Vertices[i];
                    worldVerts[i] = Vector3.Transform(new Vector3(v.X, v.Y, v.Z), world);
                }
            }

            // First pass: determine which faces are visible
            var faceVisible = new bool[model.Faces.Count];
            for (int f = 0; f < model.Faces.Count; f++)
            {
                faceVisible[f] = IsFaceVisible(model.Faces[f], model, shipToCameraLocal, worldVerts, cameraPosition, world);
            }

            // Second pass: mark edges from visible faces as solid
            foreach (var (face, idx) in model.Faces.Select((f, i) => (f, i)))
            {
                if (!faceVisible[idx]) continue;

                for (int i = 0; i < face.VertexIndices.Length; i++)
                {
                    int a = face.VertexIndices[i];
                    int b = face.VertexIndices[(i + 1) % face.VertexIndices.Length];

                    for (int e = 0; e < model.Edges.Count; e++)
                    {
                        if ((model.Edges[e].Start == a && model.Edges[e].End == b) ||
                            (model.Edges[e].Start == b && model.Edges[e].End == a))
                        {
                            edgeVisible[e] = true;
                            edgeHasFace[e] = true;
                        }
                    }
                }
            }

            // Third pass: mark edges that belong to culled faces only
            // (they have faces but no visible faces → should be dashed)
            foreach (var (face, idx) in model.Faces.Select((f, i) => (f, i)))
            {
                if (faceVisible[idx]) continue;

                for (int i = 0; i < face.VertexIndices.Length; i++)
                {
                    int a = face.VertexIndices[i];
                    int b = face.VertexIndices[(i + 1) % face.VertexIndices.Length];

                    for (int e = 0; e < model.Edges.Count; e++)
                    {
                        if ((model.Edges[e].Start == a && model.Edges[e].End == b) ||
                            (model.Edges[e].Start == b && model.Edges[e].End == a))
                        {
                            edgeHasFace[e] = true;
                        }
                    }
                }
            }

            // Edges not part of any face are always visible (structural edges)
            for (int e = 0; e < model.Edges.Count; e++)
            {
                if (!edgeHasFace[e])
                    edgeVisible[e] = true;
            }

            // Silhouette edges: build edge-to-face adjacency.
            // A face is adjacent to an edge if both edge vertices are in the face.
            var edgeFaces = new List<int>[model.Edges.Count];
            for (int e = 0; e < model.Edges.Count; e++)
                edgeFaces[e] = new List<int>();

            // Build a set of vertex indices for each face for fast lookup
            var faceVertexSets = new HashSet<int>[model.Faces.Count];
            for (int f = 0; f < model.Faces.Count; f++)
            {
                faceVertexSets[f] = new HashSet<int>(model.Faces[f].VertexIndices);
            }

            // For each edge, find all faces that contain both vertices
            for (int e = 0; e < model.Edges.Count; e++)
            {
                int start = model.Edges[e].Start;
                int end = model.Edges[e].End;
                for (int f = 0; f < model.Faces.Count; f++)
                {
                    if (faceVertexSets[f].Contains(start) && faceVertexSets[f].Contains(end))
                    {
                        edgeFaces[e].Add(f);
                    }
                }
            }

            // Edge is silhouette if it has both visible and hidden adjacent faces
            for (int e = 0; e < model.Edges.Count; e++)
            {
                if (edgeVisible[e]) continue;
                if (edgeFaces[e].Count == 0) continue;

                int visibleCount = edgeFaces[e].Count(v => faceVisible[v]);
                int hiddenCount = edgeFaces[e].Count - visibleCount;

                if (visibleCount > 0 && hiddenCount > 0)
                {
                    edgeVisible[e] = true;
                }
            }

            // Screen-space outline: project all vertices to 2D, compute the convex
            // hull of the projected points. Edges on the hull boundary are outline
            // edges and should always be solid (even if their faces are culled).
            // This ensures the wireframe always has a clean outer silhouette.
            var projectedValid = new Vector2?[model.Vertices.Count];
            for (int i = 0; i < model.Vertices.Count; i++)
            {
                var p = Project(model.Vertices[i], world, view, projection);
                if (!float.IsNaN(p.X))
                    projectedValid[i] = p;
            }

            // For each edge with only culled faces, check if it's on the 2D outline.
            // An edge is on the outline if all other projected vertices lie on one
            // side of the line defined by the edge's projected endpoints.
            for (int e = 0; e < model.Edges.Count; e++)
            {
                if (edgeVisible[e]) continue;
                if (edgeFaces[e].Count == 0) continue;
                if (edgeFaces[e].Any(f => faceVisible[f])) continue;

                int a = model.Edges[e].Start;
                int b = model.Edges[e].End;
                if (!projectedValid[a].HasValue || !projectedValid[b].HasValue) continue;

                Vector2 pa = projectedValid[a].Value;
                Vector2 pb = projectedValid[b].Value;

                // Edge direction in screen space
                Vector2 edgeDir = pb - pa;
                if (edgeDir.LengthSquared() < 1f) continue;

                // Check if all other vertices are on one side of the edge line
                bool? hasPositive = null;
                bool onOutline = true;
                for (int i = 0; i < model.Vertices.Count && onOutline; i++)
                {
                    if (i == a || i == b) continue;
                    if (!projectedValid[i].HasValue) continue;

                    Vector2 toVertex = projectedValid[i].Value - pa;
                    float cross = edgeDir.X * toVertex.Y - edgeDir.Y * toVertex.X;

                    if (Math.Abs(cross) > 1f) // Not exactly on the line
                    {
                        bool positive = cross > 0;
                        if (hasPositive.HasValue && hasPositive.Value != positive)
                        {
                            // Vertices on both sides → not an outline edge
                            onOutline = false;
                        }
                        else if (!hasPositive.HasValue)
                        {
                            hasPositive = positive;
                        }
                    }
                }

                if (onOutline)
                    edgeVisible[e] = true;
            }
        }
        else
        {
            // No culling — all edges visible
            for (int e = 0; e < model.Edges.Count; e++)
                edgeVisible[e] = true;
        }

        // Draw edges: visible solid, hidden dashed (if drawHiddenEdges), highlighted in red
        for (int i = 0; i < model.Edges.Count; i++)
        {
            var edge = model.Edges[i];
            var start = projected[edge.Start];
            var end = projected[edge.End];

            if (float.IsNaN(start.X) || float.IsNaN(end.X)) continue;

            Color drawColor = (i == highlightedEdgeIndex) ? Color.Red : (edge.Color ?? _primaryColor);
            if (drawWhite) drawColor = Color.White;

            if (edgeVisible[i])
            {
                DrawLine(spriteBatch, start, end, drawColor);
            }
            else if (drawHiddenEdges)
            {
                DrawDashedLine(spriteBatch, start, end, drawColor);
            }
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
    {
        Vector2 direction = end - start;
        float length = direction.Length();
        if (length < 0.001f) return;

        float rotation = MathF.Atan2(direction.Y, direction.X);

        spriteBatch.Draw(
            _lineTexture,
            start,
            null,
            color,
            rotation,
            new Vector2(0, 0.5f),
            new Vector2(length, 1),
            SpriteEffects.None,
            0
        );
    }

    private void DrawDashedLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
    {
        Vector2 direction = end - start;
        float length = direction.Length();
        if (length < 0.001f) return;

        float rotation = MathF.Atan2(direction.Y, direction.X);
        Vector2 unit = direction / length;

        float pos = 0;
        bool dash = true;

        while (pos < length)
        {
            float segLen = dash ? DashLength : GapLength;
            float segEnd = Math.Min(pos + segLen, length);
            float segActual = segEnd - pos;

            if (dash && segActual > 0.5f)
            {
                Vector2 segStart = start + unit * pos;
                spriteBatch.Draw(
                    _lineTexture,
                    segStart,
                    null,
                    color,
                    rotation,
                    new Vector2(0, 0.5f),
                    new Vector2(segActual, 1),
                    SpriteEffects.None,
                    0
                );
            }

            pos = segEnd;
            dash = !dash;
        }
    }

    /// <summary>
    /// Draw a circle outline (convenience method wrapping CircleRenderer).
    /// </summary>
    public void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int stepCount = 32)
    {
        var circleRenderer = new CircleRenderer(_graphicsDevice);
        circleRenderer.DrawCircle(spriteBatch, center, radius, color, stepCount);
    }

    /// <summary>
    /// Draw an axis-aligned ellipse outline (convenience method wrapping EllipseRenderer).
    /// </summary>
    public void DrawEllipse(SpriteBatch spriteBatch, Vector2 center, float radiusX, float radiusY, Color color, int stepCount = 32)
    {
        var ellipseRenderer = new EllipseRenderer(_graphicsDevice);
        ellipseRenderer.DrawAxisAlignedEllipse(spriteBatch, center, radiusX, radiusY, color, stepCount);
    }
}
