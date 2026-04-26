using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Scenes;

public class SpaceScene : GameScene
{
    private WireframeRenderer _wireframeRenderer = null!;
    private ShipModel _shipModel = null!;
    private Matrix _world;
    private Matrix _view;
    private Matrix _projection;
    private BitmapFont _font = null!;
    private Vector3 _rotation = Vector3.Zero;
    private float _distance = 3f;
    private GraphicsDevice? _graphicsDevice;
    private bool _paused;
    private bool _prevSpaceState;

    public override void LoadContent(ContentManager content, BitmapFont font)
    {
        _font = font;
        _shipModel = ShipModel.CreateCube(1.5f);
        _world = Matrix.Identity;
    }

    private void EnsureInitialized(SpriteBatch spriteBatch)
    {
        if (_graphicsDevice != null) return;

        _graphicsDevice = spriteBatch.GraphicsDevice;
        _wireframeRenderer = new WireframeRenderer(_graphicsDevice);
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(75f),
            _graphicsDevice.Viewport.AspectRatio,
            0.1f, 1000f);
        _view = Matrix.CreateLookAt(new Vector3(0, 0, _distance), Vector3.Zero, Vector3.Up);
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        System.Diagnostics.Debug.WriteLine($"SpaceScene Update! P={kb.IsKeyDown(Keys.P)}, Space={kb.IsKeyDown(Keys.Space)}, Paused={_paused}");

        // Toggle pause on P key press
        if (kb.IsKeyDown(Keys.P) && !_prevSpaceState)
        {
            _paused = !_paused;
        }
        // Also support Space
        if (kb.IsKeyDown(Keys.Space) && !_prevSpaceState)
        {
            _paused = !_paused;
        }
        _prevSpaceState = kb.IsKeyDown(Keys.P) || kb.IsKeyDown(Keys.Space);

        if (!_paused)
        {
            float speed = 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (kb.IsKeyDown(Keys.Left)) _rotation.Y -= speed;
            if (kb.IsKeyDown(Keys.Right)) _rotation.Y += speed;
            if (kb.IsKeyDown(Keys.Up)) _rotation.X -= speed;
            if (kb.IsKeyDown(Keys.Down)) _rotation.X += speed;
            if (kb.IsKeyDown(Keys.Q)) _rotation.Z -= speed;
            if (kb.IsKeyDown(Keys.W)) _rotation.Z += speed;

            if (kb.IsKeyDown(Keys.OemPlus) || kb.IsKeyDown(Keys.Add)) _distance -= speed;
            if (kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract)) _distance += speed;
            _distance = MathHelper.Clamp(_distance, 2f, 20f);
        }

        _world = Matrix.CreateRotationX(_rotation.X) *
                 Matrix.CreateRotationY(_rotation.Y) *
                 Matrix.CreateRotationZ(_rotation.Z);

        _view = Matrix.CreateLookAt(new Vector3(0, 0, _distance), Vector3.Zero, Vector3.Up);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        EnsureInitialized(spriteBatch);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _wireframeRenderer!.Draw(_shipModel, _world, _view, _projection, spriteBatch);
        spriteBatch.End();

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        _font.DrawString(spriteBatch, "SPACE VIEW", new Vector2(10, 10), Color.Lime, 1.5f);
        _font.DrawString(spriteBatch, "Arrows: Rotate  Q/W: Roll  +/-: Zoom  Space: Pause", new Vector2(10, 35), Color.White, 1f);
        _font.DrawString(spriteBatch, $"Dist: {_distance:F1}", new Vector2(10, 55), Color.Yellow, 1f);
        if (_paused)
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(10, 75), Color.Red, 1.5f);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
    }
}
