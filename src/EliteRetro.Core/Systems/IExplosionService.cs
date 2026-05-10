using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Audio;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Interface for managing the lifecycle and rendering of explosion effects.
/// </summary>
public interface IExplosionService : IDisposable
{
    /// <summary>
    /// Update all active explosions and check for new ones in the bubble.
    /// </summary>
    void Update(GameTime gameTime, IBubbleManager bubbleManager, IAudioManager audio);

    /// <summary>
    /// Render all active explosions.
    /// </summary>
    void Draw(SpriteBatch spriteBatch, Func<Vector3, Vector2> projectFn, Func<Vector3, float> distanceFn, Func<Vector3, bool> isVisibleFn, bool drawWhite);

    /// <summary>
    /// Manually trigger an explosion at a position.
    /// </summary>
    void TriggerExplosion(ShipModel model, Vector3 worldPosElite, object? tag = null);

    /// <summary>
    /// Clear all active explosions.
    /// </summary>
    void Clear();
}
