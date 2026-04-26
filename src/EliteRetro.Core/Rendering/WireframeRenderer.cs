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
    /// Uses pre-computed face normal (from Elite blueprints) when available,
    /// otherwise computes it at runtime using Newell's method.
    /// </summary>
    private bool IsFaceVisible(Face face, ShipModel model, Matrix world, Matrix view)
    {
        if (face.VertexIndices.Length < 3) return false;

        // Transform face vertices to world space
        var worldVerts = new Vector3[face.VertexIndices.Length];
        int n = face.VertexIndices.Length;
        for (int i = 0; i < n; i++)
        {
            var v = model.Vertices[face.VertexIndices[i]];
            worldVerts[i] = Vector3.Transform(new Vector3(v.X, v.Y, v.Z), world);
        }

        // Get normal: use pre-computed if available, otherwise compute with Newell's method
        Vector3 normal;
        if (face.Normal.HasValue)
        {
            // Transform pre-computed model-space normal to world space
            normal = Vector3.TransformNormal(face.Normal.Value, world);
        }
        else
        {
            // Compute normal using Newell's method (works for any polygon)
            normal = Vector3.Zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 current = worldVerts[i];
                Vector3 next = worldVerts[(i + 1) % n];
                normal.X += (current.Y - next.Y) * (current.Z + next.Z);
                normal.Y += (current.Z - next.Z) * (current.X + next.X);
                normal.Z += (current.X - next.X) * (current.Y + next.Y);
            }
        }

        // Compute face center in world space
        Vector3 faceCenter = Vector3.Zero;
        for (int i = 0; i < n; i++)
            faceCenter += worldVerts[i];
        faceCenter /= n;

        // Ensure normal points away from ship center (outward)
        // For convex ships, the face center should be in the same direction as the normal
        // If dot(normal, faceCenter) < 0, the normal points inward → flip it
        if (!face.Normal.HasValue && Vector3.Dot(normal, faceCenter) < 0)
        {
            normal = -normal;
        }

        // Camera position in world space
        Matrix invView = Matrix.Invert(view);
        Vector3 cameraPosition = invView.Translation;

        // Line of sight: from face center to camera
        Vector3 lineOfSight = cameraPosition - faceCenter;

        // Face is visible if normal points toward camera
        // Our LoS goes from face to camera. When normal also points toward camera,
        // dot(normal, LoS) > 0 means face looks at camera.
        float dot = Vector3.Dot(normal, lineOfSight);
        return dot > 0;
    }

    /// <summary>
    /// Draw a ship wireframe. Visible edges are solid, hidden edges are dashed.
    /// </summary>
    public void Draw(ShipModel model, Matrix world, Matrix view, Matrix projection, SpriteBatch spriteBatch, bool useBackFaceCulling = true)
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
            // First pass: determine which faces are visible
            var faceVisible = new bool[model.Faces.Count];
            for (int f = 0; f < model.Faces.Count; f++)
            {
                faceVisible[f] = IsFaceVisible(model.Faces[f], model, world, view);
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

            // Silhouette edges: edges belonging only to culled faces but facing the camera
            // For convex ships, these are the boundary between visible and hidden parts
            Matrix invView = Matrix.Invert(view);
            Vector3 cameraPos = invView.Translation;
            for (int e = 0; e < model.Edges.Count; e++)
            {
                if (edgeVisible[e]) continue; // already solid
                if (!edgeHasFace[e]) continue; // structural, already handled

                // Compute edge midpoint in world space
                var edge = model.Edges[e];
                Vector3 startW = Vector3.Transform(new Vector3(model.Vertices[edge.Start].X, model.Vertices[edge.Start].Y, model.Vertices[edge.Start].Z), world);
                Vector3 endW = Vector3.Transform(new Vector3(model.Vertices[edge.End].X, model.Vertices[edge.End].Y, model.Vertices[edge.End].Z), world);
                Vector3 midPoint = (startW + endW) / 2;

                // Edge direction
                Vector3 edgeDir = Vector3.Normalize(endW - startW);

                // Vector from ship center (origin in model space, transformed to world)
                Vector3 shipCenter = Vector3.Transform(Vector3.Zero, world);
                Vector3 radialDir = Vector3.Normalize(midPoint - shipCenter);

                // Edge "normal" — perpendicular to both edge direction and radial direction
                Vector3 edgeNormal = Vector3.Cross(edgeDir, radialDir);
                if (edgeNormal.Length() < 0.001f) continue; // edge points radially, skip
                edgeNormal = Vector3.Normalize(edgeNormal);

                // Vector from edge midpoint to camera
                Vector3 toCamera = Vector3.Normalize(cameraPos - midPoint);

                // If edge normal points toward camera, it's a silhouette edge → make solid
                if (Vector3.Dot(edgeNormal, toCamera) > 0)
                {
                    edgeVisible[e] = true;
                }
            }
        }
        else
        {
            // No culling — all edges visible
            for (int e = 0; e < model.Edges.Count; e++)
                edgeVisible[e] = true;
        }

        // Draw edges: visible solid, hidden dashed
        for (int i = 0; i < model.Edges.Count; i++)
        {
            var edge = model.Edges[i];
            var start = projected[edge.Start];
            var end = projected[edge.End];

            if (float.IsNaN(start.X) || float.IsNaN(end.X)) continue;

            if (edgeVisible[i])
            {
                DrawLine(spriteBatch, start, end, edge.Color ?? _primaryColor);
            }
            else
            {
                DrawDashedLine(spriteBatch, start, end, edge.Color ?? _primaryColor);
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
}
