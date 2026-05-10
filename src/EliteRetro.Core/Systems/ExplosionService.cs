using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Audio;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Service to manage ship and entity explosion effects.
/// Decouples the scene from tracking and timing of individual explosion clouds.
/// </summary>
public class ExplosionService : IExplosionService
{
    private readonly ExplosionRenderer _renderer;
    private readonly List<ExplosionRenderer.ExplosionCloud> _explosions = new();
    private bool _disposed;

    public ExplosionService(GraphicsDevice graphicsDevice)
    {
        _renderer = new ExplosionRenderer(graphicsDevice);
    }

    public void Update(GameTime gameTime, IBubbleManager bubbleManager, IAudioManager audio)
    {
        // 1. Scan for new explosions from inactive entities in the bubble
        for (int i = GameConstants.FirstAvailableSlot; i < GameConstants.MaxSlots; i++)
        {
            var entity = bubbleManager.GetSlot(i);
            // If entity is inactive but not yet being tracked by an explosion cloud
            if (entity != null && !entity.IsActive && !_explosions.Any(e => e.Tag == entity))
            {
                audio.PlayExplosion();
                var cloud = _renderer.CreateExplosion(entity.Blueprint.Model, entity.Position);
                cloud.Tag = entity;
                _explosions.Add(cloud);
            }
        }

        // 2. Update existing explosions and cleanup
        _explosions.RemoveAll(cloud =>
        {
            // Hard TTL: 3 seconds max
            if (cloud.AgeSeconds >= 3f)
            {
                if (cloud.Tag is ShipInstance taggedTtl)
                    bubbleManager.Despawn(taggedTtl.SlotIndex, "explosion ttl");
                return true;
            }

            // Standard lifecycle: expand then shrink
            // We use a dummy SpriteBatch for the update logic (UpdateAndDraw handles both)
            // But we'll split it or just call it without drawing in future if needed.
            // For now, we update AgeSeconds and Counter manually or via renderer.
            
            // NOTE: ExplosionRenderer.UpdateAndDraw returns false when Counter <= 0
            // We'll let Draw handle the actual renderer call.
            
            // Increment AgeSeconds here
            cloud.AgeSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Lifecycle logic matches ExplosionRenderer.UpdateAndDraw
            if (cloud.Counter < 128) cloud.Counter += 4;
            else cloud.Counter -= 8;

            if (cloud.Counter <= 0)
            {
                cloud.CleanupDelayFrames--;
                if (cloud.CleanupDelayFrames <= 0 && cloud.Tag is ShipInstance tagged)
                {
                    bubbleManager.Despawn(tagged.SlotIndex, "explosion complete");
                    return true;
                }
            }
            return false;
        });
    }

    public void Draw(SpriteBatch spriteBatch, Func<Vector3, Vector2> projectFn, Func<Vector3, float> distanceFn, Func<Vector3, bool> isVisibleFn, bool drawWhite)
    {
        foreach (var cloud in _explosions)
        {
            if (cloud.Counter <= 0 || !isVisibleFn(cloud.WorldPosElite))
                continue;

            // Project 3D elite position to 2D screen
            cloud.Center = projectFn(cloud.WorldPosElite);
            cloud.Distance = Math.Max(distanceFn(cloud.WorldPosElite), 0.5f);

            // Draw the cloud using existing renderer logic
            // Note: we've already updated the counter in Update()
            _renderer.UpdateAndDraw(spriteBatch, cloud, new GameTime(), drawWhite);
        }
    }

    public void TriggerExplosion(ShipModel model, Vector3 worldPosElite, object? tag = null)
    {
        var cloud = _renderer.CreateExplosion(model, worldPosElite);
        cloud.Tag = tag;
        _explosions.Add(cloud);
    }

    public void Clear()
    {
        _explosions.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // ExplosionRenderer currently doesn't have a Dispose, but if it adds textures later...
    }
}
