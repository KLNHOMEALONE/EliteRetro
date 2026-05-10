using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Managers;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Service for projecting and rendering celestial bodies (planets, suns).
/// Handles painters algorithm for correct front/back drawing order and eclipse logic.
/// </summary>
public interface ICelestialService : IDisposable
{
    /// <summary>
    /// Render celestial bodies in the local bubble with correct ordering and projection.
    /// </summary>
    void Draw(SpriteBatch spriteBatch, IBubbleManager bubbleManager, Matrix view, Matrix projection, Vector3 cameraLookDir, GraphicsDevice graphicsDevice, float hudHeightFraction, bool drawWhite);
}
