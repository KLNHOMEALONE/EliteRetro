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
    private CircleRenderer _circleRenderer = null!;
    private PlanetRenderer _planetRenderer = null!;
    private SunRenderer _sunRenderer = null!;
    private RingRenderer _ringRenderer = null!;
    private int _planetRotation;
    private int _planetRotationCounter;
    private Matrix _view;
    private Matrix _projection;
    private BitmapFont _font = null!;
    private GraphicsDevice? _graphicsDevice;
    private GameInstance _gameInstance = null!;
    private LocalBubbleManager _bubbleManager = null!;
    private float _cameraDistance = 80f;
    private FlightController _flightController = null!;
    private OrientationMatrix _universeOrientation = OrientationMatrix.Identity;
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
        _circleRenderer = new CircleRenderer(_graphicsDevice);
        _planetRenderer = new PlanetRenderer(_graphicsDevice);
        _sunRenderer = new SunRenderer(_graphicsDevice);
        _ringRenderer = new RingRenderer(_graphicsDevice);
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
                Position = new Vector3(-GameConstants.PlanetRadius * 3, 0, -GameConstants.PlanetRadius * 5),
                Speed = 0
            };
            _bubbleManager.SetSlot(GameConstants.PlanetSlot, planet);

            // Slot 1: Sun - much larger, placed more centrally
            var sunModel = SunModel.Create(GameConstants.PlanetRadius * 80);
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
                Position = new Vector3(0, 0, -GameConstants.PlanetRadius * 20),
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
            // Apply Minsky rotation to the player's orientation
            _universeOrientation.ApplyUniverseRotation(-_flightController.RollAngle, -_flightController.PitchAngle);

            // Periodic TIDY orthonormalization
            _tidyCounter++;
            if (_tidyCounter >= 60)
            {
                _tidyCounter = 0;
                _universeOrientation.Tidy();
            }
            _bubbleManager.TidyOne();

            // Slow planet rotation (1 full rotation every ~32 seconds at 60fps)
            if (_planetRotationCounter++ % 32 == 0)
                _planetRotation = (_planetRotation + 1) % 64;

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

        // View matrix: camera at origin, looking forward along -Z.
        // The player's orientation rotates the view direction.
        // Build view matrix directly from orientation basis:
        // In camera space, right = +X, up = +Y, forward = -Z.
        // The orientation gives us: sidev = right, roofv = up, nosev = forward.
        // View matrix = [sidev; roofv; -nosev] (3x3 rotation part).
        // This is the transpose/inverse of [sidev | roofv | -nosev] as columns.
        Vector3 side = _universeOrientation.Sidev;
        Vector3 roof = _universeOrientation.Roofv;
        Vector3 nose = _universeOrientation.Nosev;
        // View matrix: rows are camera axes in world space
        // Camera right = sidev, camera up = roofv, camera forward (toward -Z) = -nosev
        _view = new Matrix(
            side.X, side.Y, side.Z, 0,
            roof.X, roof.Y, roof.Z, 0,
            -nose.X, -nose.Y, -nose.Z, 0,
            0, 0, 0, 1);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        EnsureInitialized(spriteBatch);

        if (_graphicsDevice != null)
            _graphicsDevice.Clear(Color.Black);

        if (!_initialized)
            return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Draw a reference cube (small, at moderate distance)
        var cube = ShipModel.CreateCube(0.5f);
        Matrix cubeWorld = Matrix.CreateTranslation(0, 0, -30);
        _wireframeRenderer.Draw(cube, cubeWorld, _view, _projection, spriteBatch);

        // Render bubble entities (skip planet and sun - rendered separately)
        foreach (var entity in _bubbleManager.GetAllActive())
        {
            if (entity.Blueprint?.Model != null &&
                entity.Blueprint.Name != "Planet" &&
                entity.Blueprint.Name != "Sun")
            {
                Matrix entityWorld = Matrix.CreateScale(0.0001f) *
                                     Matrix.CreateTranslation(entity.Position * 0.0001f);
                _wireframeRenderer.Draw(entity.Blueprint.Model, entityWorld, _view, _projection, spriteBatch);
            }
        }

        // Draw planet with surface features
        if (_bubbleManager.Planet != null)
        {
            // Draw back half of rings first (behind planet)
            DrawCelestialRings(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(180, 160, 120), "back");

            // Draw planet on top of back rings
            DrawCelestialPlanet(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(50, 100, 180));

            // Draw front half of rings on top of planet
            DrawCelestialRings(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(180, 160, 120), "front");
        }

        // Draw sun with scan lines and fringe
        if (_bubbleManager.SunOrStation != null && _bubbleManager.SunOrStation.Blueprint?.Name == "Sun")
        {
            DrawCelestialSun(spriteBatch, _bubbleManager.SunOrStation.Position, GameConstants.PlanetRadius * 6, SunRenderer.GetSunColor(0));
        }

        _font.DrawString(spriteBatch, "SPACE VIEW", new Vector2(10, 10), Color.Lime, 1.5f);
        _font.DrawString(spriteBatch, $"Entities: {_bubbleManager.GetAllActive().Count()}", new Vector2(10, 30), Color.Cyan, 1f);
        _font.DrawString(spriteBatch, $"Cam: ({_view.Translation.X:F1}, {_view.Translation.Y:F1}, {_view.Translation.Z:F1})", new Vector2(10, 300), Color.Magenta, 1f);
        _font.DrawString(spriteBatch, $"Nose: ({_universeOrientation.Nosev.X:F2}, {_universeOrientation.Nosev.Y:F2}, {_universeOrientation.Nosev.Z:F2})", new Vector2(10, 320), Color.Yellow, 1f);
        _font.DrawString(spriteBatch, "Arrows U/D: Pitch  Q/W: Roll  +/-: Zoom  P: Pause  T: Station  V: View", new Vector2(10, 50), Color.White, 1f);
        if (_paused)
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(10, 70), Color.Red, 1.5f);
        spriteBatch.End();
    }

    private void DrawCelestialRings(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color, string layer = "all")
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
            _ringRenderer.DrawAxisAlignedRings(spriteBatch, new Vector2(screenX, screenY), screenRadius, 1.4f, 2.2f, color, layer);
    }

    private void DrawCelestialSun(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color)
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
            _sunRenderer.DrawSun(spriteBatch, new Vector2(screenX, screenY), screenRadius, color);
    }

    private void DrawCelestialCircle(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color)
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
            _circleRenderer.DrawCircle(spriteBatch, new Vector2(screenX, screenY), screenRadius, color);
    }

    private void DrawCelestialPlanet(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color)
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
            _planetRenderer.DrawPlanet(spriteBatch, new Vector2(screenX, screenY), screenRadius, color, _planetRotation);
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

    public override void UnloadContent() { }
}
