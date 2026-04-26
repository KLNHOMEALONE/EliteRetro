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
    /// </summary>
    private bool IsFaceVisible(Face face, ShipModel model, Matrix world, Matrix view)
    {
        if (face.VertexIndices.Length < 3) return false;

        // Transform all face vertices to world space
        var worldVerts = new Vector3[face.VertexIndices.Length];
        for (int i = 0; i < face.VertexIndices.Length; i++)
        {
            var v = model.Vertices[face.VertexIndices[i]];
            worldVerts[i] = Vector3.Transform(new Vector3(v.X, v.Y, v.Z), world);
        }

        // Compute face normal using Newell's method (works for any polygon)
        Vector3 normal = Vector3.Zero;
        int n = worldVerts.Length;
        for (int i = 0; i < n; i++)
        {
            Vector3 current = worldVerts[i];
            Vector3 next = worldVerts[(i + 1) % n];
            normal.X += (current.Y - next.Y) * (current.Z + next.Z);
            normal.Y += (current.Z - next.Z) * (current.X + next.X);
            normal.Z += (current.X - next.X) * (current.Y + next.Y);
        }

        // Camera position in world space
        Vector3 cameraPosition = Vector3.Transform(Vector3.Zero, Matrix.Invert(view));

        // Vector from face center to camera
        Vector3 faceCenter = Vector3.Zero;
        foreach (var p in worldVerts) faceCenter += p;
        faceCenter /= n;
        Vector3 toCamera = cameraPosition - faceCenter;

        // Face is visible if normal points toward camera
        return Vector3.Dot(normal, toCamera) > 0;
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

        if (useBackFaceCulling && model.Faces.Count > 0)
        {
            foreach (var face in model.Faces)
            {
                if (!IsFaceVisible(face, model, world, view)) continue;

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
                        }
                    }
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
