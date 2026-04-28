using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Systems;

namespace EliteRetro.Core.Scenes;

public class SpaceScene : GameScene
{
    private WireframeRenderer _wireframeRenderer = null!;
    private Matrix _view;
    private Matrix _projection;
    private BitmapFont _font = null!;
    private GraphicsDevice? _graphicsDevice;
    private GameInstance _gameInstance = null!;
    private LocalBubbleManager _bubbleManager = null!;
    private float _cameraDistance = 80f;
    private FlightController _flightController = null!;
    private OrientationMatrix _universeOrientation;
    private bool _paused;
    private bool _prevSpaceState;
    private bool _prevT;
    private bool _initialized;
    private Texture2D _pixelTexture = null!;
    private int _tidyCounter;

    public SpaceScene(Game? game = null)
    {
        if (game is GameInstance gi)
        {
            _gameInstance = gi;
            _bubbleManager = gi.BubbleManager;
        }
        _flightController = new FlightController();
    }

    public override void LoadContent(ContentManager content, BitmapFont font)
    {
        _font = font;
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
        _view = Matrix.CreateLookAt(new Vector3(0, 0, _cameraDistance), Vector3.Zero, Vector3.Up);

        // Create 1x1 white pixel texture for drawing celestial bodies
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        if (!_initialized && _gameInstance != null)
        {
            _bubbleManager.Clear();

            // Slot 0: Planet
            var planetModel = PlanetModel.Create(GameConstants.PlanetRadius);
            var planetBlueprint = new ShipBlueprint
            {
                Name = "Planet",
                Model = planetModel,
                MaxSpeed = 0,
                MaxEnergy = 255,
                HullStrength = 255,
                ShieldStrength = 255
            };
            var planet = new ShipInstance(planetBlueprint)
            {
                Position = new Vector3(0, 0, -GameConstants.PlanetRadius),
                Speed = 0
            };
            _bubbleManager.SetSlot(GameConstants.PlanetSlot, planet);

            // Slot 1: Sun - placed off to the side for visibility
            var sunModel = SunModel.Create(GameConstants.PlanetRadius * 3);
            var sunBlueprint = new ShipBlueprint
            {
                Name = "Sun",
                Model = sunModel,
                MaxSpeed = 0,
                MaxEnergy = 255,
                HullStrength = 255,
                ShieldStrength = 255
            };
            var sun = new ShipInstance(sunBlueprint)
            {
                Position = new Vector3(30000, 0, -50000),
                Speed = 0
            };
            _bubbleManager.SetSlot(GameConstants.SunStationSlot, sun);

            _initialized = true;
        }
    }

    public override void Update(GameTime gameTime)
    {
        _flightController.Update(gameTime);

        var kb = Keyboard.GetState();

        if (!_flightController.IsPaused)
        {
            // Apply Minsky universe rotation to all entities
            // Universe rotates opposite to player: pitch up = universe down, roll right = universe left
            _universeOrientation.ApplyUniverseRotation(-_flightController.RollAngle, -_flightController.PitchAngle);
            _bubbleManager.ApplyUniverseRotation(-_flightController.RollAngle, -_flightController.PitchAngle);

            // Periodic TIDY orthonormalization
            _tidyCounter++;
            if (_tidyCounter >= 60)
            {
                _tidyCounter = 0;
                _universeOrientation.Tidy();
            }
            _bubbleManager.TidyOne();

            // Zoom with +/-
            float speed = 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.OemPlus) || kb.IsKeyDown(Keys.Add)) _cameraDistance -= speed;
            if (kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract)) _cameraDistance += speed;
            _cameraDistance = MathHelper.Clamp(_cameraDistance, 2f, 20f);

            // Debug: T key for station spawn
            if (kb.IsKeyDown(Keys.T) && !_prevT)
            {
                if (_bubbleManager.SunOrStation?.Blueprint?.Name == "Sun")
                    SpawnStation();
            }
            _prevT = kb.IsKeyDown(Keys.T);
        }
        else
        {
            // Debug: T key still works when paused
            if (kb.IsKeyDown(Keys.T) && !_prevT)
            {
                if (_bubbleManager.SunOrStation?.Blueprint?.Name == "Sun")
                    SpawnStation();
            }
            _prevT = kb.IsKeyDown(Keys.T);
        }

        _view = Matrix.CreateLookAt(new Vector3(0, 0, _cameraDistance), Vector3.Zero, Vector3.Up);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        EnsureInitialized(spriteBatch);

        if (_graphicsDevice != null)
            _graphicsDevice.Clear(Color.Black);

        if (!_initialized)
            return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Draw a reference cube at origin to show rotation
        var cube = ShipModel.CreateCube(0.5f);
        _wireframeRenderer.Draw(cube, _universeOrientation.ToMatrix4x4(), _view, _projection, spriteBatch);

        // Render bubble entities (skip planet and sun - rendered separately)
        foreach (var entity in _bubbleManager.GetAllActive())
        {
            if (entity.Blueprint?.Model != null &&
                entity.Blueprint.Name != "Planet" &&
                entity.Blueprint.Name != "Sun")
            {
                Matrix entityWorld = Matrix.CreateScale(0.0001f) *
                                     _universeOrientation.ToMatrix4x4() *
                                     Matrix.CreateTranslation(entity.Position * 0.0001f);
                _wireframeRenderer.Draw(entity.Blueprint.Model, entityWorld, _view, _projection, spriteBatch);
            }
        }

        // Draw planet as filled circle (temporary until PlanetRenderer is implemented)
        if (_bubbleManager.Planet != null)
        {
            DrawCelestialBody(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(50, 100, 180));
        }

        // Draw sun as filled circle (temporary until SunRenderer is implemented)
        if (_bubbleManager.SunOrStation != null && _bubbleManager.SunOrStation.Blueprint?.Name == "Sun")
        {
            DrawCelestialBody(spriteBatch, _bubbleManager.SunOrStation.Position, GameConstants.PlanetRadius * 3, new Color(255, 200, 50));
        }

        _font.DrawString(spriteBatch, "SPACE VIEW", new Vector2(10, 10), Color.Lime, 1.5f);
        _font.DrawString(spriteBatch, $"Entities: {_bubbleManager.GetAllActive().Count()}", new Vector2(10, 30), Color.Cyan, 1f);
        _font.DrawString(spriteBatch, "Arrows U/D: Pitch  Q/W: Roll  +/-: Zoom  P: Pause  T: Station  V: View", new Vector2(10, 50), Color.White, 1f);
        if (_paused)
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(10, 70), Color.Red, 1.5f);
        spriteBatch.End();
    }

    private void DrawCelestialBody(SpriteBatch sb, Vector3 worldPos, float radius, Color color)
    {
        Vector3 pos = worldPos * 0.0001f;
        Vector3 projected = Vector3.Transform(pos, _view * _projection);
        if (projected.Z == 0) return;

        float ndcX = projected.X / projected.Z;
        float ndcY = projected.Y / projected.Z;
        var viewport = _graphicsDevice.Viewport;
        float screenX = (ndcX + 1) / 2 * viewport.Width;
        float screenY = (1 - ndcY) / 2 * viewport.Height;

        float screenRadius = radius * 0.0001f / Math.Abs(projected.Z) * viewport.Height / 2;
        if (screenRadius > 0 && screenRadius < 500)
            DrawFilledCircle(sb, new Vector2(screenX, screenY), screenRadius, color);
    }

    private void SpawnStation()
    {
        var stationModel = CoriolisStationModel.Create(1.0f);
        _bubbleManager.SpawnStation(new ShipBlueprint
        {
            Name = "Coriolis Station",
            Model = stationModel,
            MaxSpeed = 0,
            MaxEnergy = 255,
            HullStrength = 255,
            ShieldStrength = 255
        });
    }

    private void DrawFilledCircle(SpriteBatch sb, Vector2 center, float radius, Color color)
    {
        if (radius < 1) return;
        sb.Draw(_pixelTexture, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2)), color);
    }

    public override void UnloadContent() { }
}
