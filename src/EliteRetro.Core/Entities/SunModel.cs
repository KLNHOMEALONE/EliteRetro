using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Sun wireframe model — large sphere with radial lines for scan-line rendering.
/// Used for sun rendering in the flight scene.
/// </summary>
public class SunModel : ShipModel
{
    /// <summary>
    /// Create a sun model with given radius.
    /// Generates a sphere with equatorial and meridional lines for
    /// scan-line sun rendering with fringe effects.
    /// </summary>
    public static SunModel Create(float radius = GameConstants.PlanetRadius * 3)
    {
        var vertices = new List<Vertex3>();
        var edges = new List<Edge>();

        // Create latitude rings (horizontal lines)
        int latitudes = 8;
        int longitudeSteps = 16;

        for (int lat = 0; lat <= latitudes; lat++)
        {
            float latAngle = MathF.PI * lat / latitudes - MathF.PI / 2;
            float ringRadius = radius * MathF.Cos(latAngle);
            float y = radius * MathF.Sin(latAngle);

            int prevLonVertex = -1;
            int firstLonVertex = -1;

            for (int lon = 0; lon <= longitudeSteps; lon++)
            {
                float lonAngle = MathF.PI * 2 * lon / longitudeSteps;
                float x = ringRadius * MathF.Cos(lonAngle);
                float z = ringRadius * MathF.Sin(lonAngle);

                int vertexIndex = vertices.Count;
                vertices.Add(new Vertex3(x, y, z));

                if (prevLonVertex >= 0)
                    edges.Add(new Edge(prevLonVertex, vertexIndex, Color.Orange));

                if (firstLonVertex < 0)
                    firstLonVertex = vertexIndex;

                prevLonVertex = vertexIndex;
            }
        }

        // Create longitude lines (vertical meridians)
        int meridians = 8;
        for (int m = 0; m < meridians; m++)
        {
            float meridianAngle = MathF.PI * 2 * m / meridians;
            int prevLatVertex = -1;

            for (int lat = 0; lat <= latitudes; lat++)
            {
                float latAngle = MathF.PI * lat / latitudes - MathF.PI / 2;
                float x = radius * MathF.Cos(latAngle) * MathF.Cos(meridianAngle);
                float y = radius * MathF.Sin(latAngle);
                float z = radius * MathF.Cos(latAngle) * MathF.Sin(meridianAngle);

                int vertexIndex = vertices.Count;
                vertices.Add(new Vertex3(x, y, z));

                if (prevLatVertex >= 0)
                    edges.Add(new Edge(prevLatVertex, vertexIndex, Color.OrangeRed));

                prevLatVertex = vertexIndex;
            }
        }

        return new SunModel
        {
            Name = "Sun",
            Vertices = vertices,
            Edges = edges,
            Faces = new List<Face>(), // No faces — sun is drawn as wireframe only
        };
    }
}
