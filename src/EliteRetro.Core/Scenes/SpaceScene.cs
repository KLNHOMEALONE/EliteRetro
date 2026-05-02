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
    private FlightControlService _flightControlService = null!;
    private OrientationMatrix _universeOrientation = OrientationMatrix.Identity;
    private bool _paused;
    private bool _prevSpaceState;
    private bool _prevT;
    private bool _initialized;
    private Texture2D _pixelTexture = null!;
    private int _tidyCounter;
    private float _cumulativeRoll; // accumulated roll angle in radians, for planet/ring counter-rotation
    private int _debugHighlightedEdge = -1;
    private bool _prevUp;
    private bool _prevDown;

    public SpaceScene(Game? game = null)
    {
        if (game is GameInstance gi)
        {
            _gameInstance = gi;
            _bubbleManager = gi.BubbleManager;
        }
        _flightControlService = new FlightControlService();
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _graphicsDevice = graphicsDevice;
        _wireframeRenderer = new WireframeRenderer(_graphicsDevice);
        _circleRenderer = new CircleRenderer(_graphicsDevice);
        _planetRenderer = new PlanetRenderer(_graphicsDevice);
        _sunRenderer = new SunRenderer(_graphicsDevice);
        _ringRenderer = new RingRenderer(_graphicsDevice);
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(75f),
            _graphicsDevice.Viewport.AspectRatio,
            0.1f, 1000f);
        _view = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up);

        // Create 1x1 white pixel texture for drawing celestial bodies
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        if (_gameInstance != null)
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
        var control = _flightControlService.Update(gameTime);
        var kb = Keyboard.GetState();

        if (!control.IsPaused)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 1. ROTATE UNIVERSE (Minsky algorythm)
            // Roll and Pitch are applied to ALL entities in the universe.
            // ShipInstance.ApplyUniverseRotation uses authentic Elite MVS4 logic.
            // Signs are set for aircraft-style control (UP = Dive, planet goes UP):
            // Positive Roll (Right) -> rotate universe LEFT (negative rollDelta).
            // Positive Pitch (Up/Climb) -> rotate universe DOWN (negative pitchDelta).
            float rollDelta = Math.Clamp(control.RollAngle * dt * 60f, -0.1f, 0.1f);
            float pitchDelta = Math.Clamp(control.PitchAngle * dt * 60f, -0.1f, 0.1f);
            _bubbleManager.ApplyUniverseRotation(-rollDelta, -pitchDelta);

            // Track cumulative roll for planet/ring counter-rotation
            _cumulativeRoll += rollDelta;

            // Periodic TIDY orthonormalization to correct Minsky drift
            _bubbleManager.TidyAllActive();

            // Slow planet rotation (1 full rotation every ~32 seconds at 60fps)
            if (_planetRotationCounter++ % 32 == 0)
                _planetRotation = (_planetRotation + 1) % 64;

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

        // FIXED VIEW DIRECTIONS for Rotating Universe model (Front View).
        Vector3 forwardBasis = Vector3.UnitZ; // camera Z-basis (backwards in RH)
        Vector3 sideBasis = Vector3.UnitX;
        Vector3 upBasis = Vector3.UnitY;

        // Fixed View matrix basis vectors in COLUMNS for MonoGame v * M convention.
        _view = new Matrix(
            sideBasis.X, upBasis.X, forwardBasis.X, 0,
            sideBasis.Y, upBasis.Y, forwardBasis.Y, 0,
            sideBasis.Z, upBasis.Z, forwardBasis.Z, 0,
            0, 0, 0, 1);

        // Debug: cycle highlighted edge with Up/Down when paused
        if (control.IsPaused)
        {
            if (kb.IsKeyDown(Keys.Up) && !_prevUp)
                _debugHighlightedEdge = (_debugHighlightedEdge + 1) % 12; // cube has 12 edges
            _prevUp = kb.IsKeyDown(Keys.Up);

            if (kb.IsKeyDown(Keys.Down) && !_prevDown)
                _debugHighlightedEdge = (_debugHighlightedEdge - 1 + 12) % 12;
            _prevDown = kb.IsKeyDown(Keys.Down);
        }
        else
        {
            _debugHighlightedEdge = -1;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        EnsureInitialized();

        if (_graphicsDevice != null)
            _graphicsDevice.Clear(Color.Black);

        if (!_initialized)
            return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Draw a reference cube (small, at moderate distance)
        var cube = ShipModel.CreateCube(0.5f);
        Matrix cubeWorld = Matrix.CreateTranslation(0, 0, -30);
        _wireframeRenderer.Draw(cube, cubeWorld, _view, _projection, spriteBatch, highlightedEdgeIndex: _debugHighlightedEdge);

        // Render bubble entities (skip planet and sun - rendered separately)
        foreach (var entity in _bubbleManager.GetAllActive())
        {
            // Skip player ship
            if (entity.SlotIndex == GameConstants.PlayerSlot) continue;

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
            // Convert cumulative roll to 0-63 units (1/64-turn)
            // Negative roll (left) → planet features rotate right (counter-rotation)
            int rollAngle64 = ((int)(_cumulativeRoll * 64 / MathHelper.TwoPi) % 64 + 64) % 64;
            int totalPlanetRotation = (_planetRotation + rollAngle64) % 64;

            // Draw back half of rings first (behind planet, with counter-rotation)
            DrawCelestialRings(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(180, 160, 120), "back", rollAngle64);

            // Draw planet on top of back rings (with counter-rotation)
            DrawCelestialPlanet(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(50, 100, 180), totalPlanetRotation);

            // Draw front half of rings on top of planet (with counter-rotation)
            DrawCelestialRings(spriteBatch, _bubbleManager.Planet.Position, GameConstants.PlanetRadius, new Color(180, 160, 120), "front", rollAngle64);
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
        _font.DrawString(spriteBatch, "Arrows: Pitch/Roll  +/-: Zoom  P: Pause  T: Station  V: View", new Vector2(10, 50), Color.White, 1f);
        if (_paused)
        {
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(10, 70), Color.Red, 1.5f);
            _font.DrawString(spriteBatch, $"Edge: {_debugHighlightedEdge}  Up/Down: cycle", new Vector2(10, 90), Color.Orange, 1f);
            _font.DrawString(spriteBatch, $"sidev=({_universeOrientation.Sidev.X:F3},{_universeOrientation.Sidev.Y:F3},{_universeOrientation.Sidev.Z:F3})", new Vector2(10, 110), Color.Cyan, 0.8f);
            _font.DrawString(spriteBatch, $"roofv=({_universeOrientation.Roofv.X:F3},{_universeOrientation.Roofv.Y:F3},{_universeOrientation.Roofv.Z:F3})", new Vector2(10, 125), Color.Cyan, 0.8f);
            _font.DrawString(spriteBatch, $"nosev=({_universeOrientation.Nosev.X:F3},{_universeOrientation.Nosev.Y:F3},{_universeOrientation.Nosev.Z:F3})", new Vector2(10, 140), Color.Cyan, 0.8f);
        }
        spriteBatch.End();
    }

    private void DrawCelestialRings(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color, string layer = "all", int tiltAngle = 16)
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
            _ringRenderer.DrawAxisAlignedRings(spriteBatch, new Vector2(screenX, screenY), screenRadius, 1.4f, 2.2f, color, tiltAngle, layer);
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

    private void DrawCelestialPlanet(SpriteBatch spriteBatch, Vector3 worldPos, float radius, Color color, int rotationAngle = 0)
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
            _planetRenderer.DrawPlanet(spriteBatch, new Vector2(screenX, screenY), screenRadius, color, rotationAngle);
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
