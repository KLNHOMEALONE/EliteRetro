using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Interface for the starfield particle system (stardust).
/// Handles 3D star movement, rotation, and motion blur effects.
/// </summary>
public interface IStardustService : IDisposable
{
    /// <summary>
    /// Initialize stars with a specific seed.
    /// </summary>
    void Initialize(int seed);

    /// <summary>
    /// Update star positions based on player motion and universe rotation.
    /// </summary>
    void Update(float playerSpeed, float rollDelta, float pitchDelta, GameTime gameTime);

    /// <summary>
    /// Draw the starfield projected onto screen space.
    /// </summary>
    void Draw(SpriteBatch spriteBatch, Vector2 screenCenter, float scale, Matrix view, bool drawWhite);
}
