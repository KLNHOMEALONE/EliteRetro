using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Rendering;
using EliteRetro.Core.Systems;

namespace EliteRetro.Core.Scenes;

public class MainMenuScene : GameScene
{
    private WireframeRenderer _wireframeRenderer = null!;
    private ShipModel _cobraModel = null!;
    private BitmapFont _font = null!;
    private Game? _game;
    private IGameContext? _gameInstance;
    private Matrix _world;
    private Matrix _view;
    private Matrix _projection;
    private GraphicsDevice? _graphicsDevice;
    private int _selectedItem;
    private Texture2D _whitePixel = null!;
    private readonly string[] _menuItems = {
        "COMBAT RATING",
        "START NEW GAME",
        "LOAD GAME",
        "SPACE VIEW",
        "GALAXY MAP",
        "TOP PILOTS",
        "OPTIONS",
        "QUIT"
    };
    private bool _hasSavedGame;
    private readonly string[] _ratings = { "HARMLESS", "MOSTLY HARMLESS", "NOVICE", "COMPETENT", "EXPERT", "DANGEROUS", "DEADLY", "ELITE" };
    private string _currentRating = "DANGEROUS";
    private float _shipRotationY;
    private int _currentModelIndex;
    private int _highlightedEdgeIndex = -1;
    private bool _showHiddenEdges = true;
    private FlightControlState _lastControl;
    private readonly List<(string Name, Func<float, ShipModel> Create)> _shipModels = new();

    public MainMenuScene(IGameContext? game = null)
    {
        if (game != null)
        {
            _gameInstance = game;
            if (game is Game g)
                _game = g;
        }
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _graphicsDevice = graphicsDevice;
        _wireframeRenderer = new WireframeRenderer(_graphicsDevice);
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            _graphicsDevice.Viewport.AspectRatio,
            0.1f, 1000f);
        // Camera positioned to show ship in top 2/3
        _view = Matrix.CreateLookAt(new Vector3(0, 10f, 6), new Vector3(0, -1f, 0), Vector3.Up);

        // Check for saved game
        _hasSavedGame = SaveGameManager.SaveExists();

        _shipModels.Add(("Anaconda", size => AnacondaModel.Create(size)));
        _shipModels.Add(("Cobra Mk3", size => CobraMk3Model.Create(size)));
        _shipModels.Add(("Coriolis", size => CoriolisStationModel.Create(size / 2f)));
        _shipModels.Add(("Missile", size => MissileModel.Create(size)));
        _shipModels.Add(("Asteroid", size => AsteroidModel.Create(size)));
        _shipModels.Add(("Thargon", size => ThargonModel.Create(size)));
        _shipModels.Add(("Canister", size => CanisterModel.Create(size)));
        _shipModels.Add(("Escape Pod", size => EscapePodModel.Create(size)));
        _shipModels.Add(("Sidewinder", size => SidewinderModel.Create(size)));
        _shipModels.Add(("Viper", size => ViperModel.Create(size)));
        _shipModels.Add(("Mamba", size => MambaModel.Create(size)));
        _shipModels.Add(("Python", size => PythonModel.Create(size)));
        _shipModels.Add(("Fer-de-Lance", size => FerDeLanceModel.Create(size)));
        _shipModels.Add(("Adder", size => AdderModel.Create(size)));
        _shipModels.Add(("Asp Mk II", size => AspMk2Model.Create(size)));
        _shipModels.Add(("Boa", size => BoaModel.Create(size)));
        _shipModels.Add(("Constrictor", size => ConstrictorModel.Create(size)));
        _shipModels.Add(("Cobra Mk1", size => CobraMk1Model.Create(size)));
        _shipModels.Add(("Gecko", size => GeckoModel.Create(size)));
        _shipModels.Add(("Krait", size => KraitModel.Create(size)));
        _shipModels.Add(("Moray", size => MorayModel.Create(size)));
        _shipModels.Add(("Shuttle", size => ShuttleModel.Create(size)));
        _shipModels.Add(("Transporter", size => TransporterModel.Create(size)));
        _shipModels.Add(("Worm", size => WormModel.Create(size)));
        _shipModels.Add(("Thargoid", size => ThargoidModel.Create(size)));
        _shipModels.Add(("Cougar", size => CougarModel.Create(size)));
        _shipModels.Add(("Dodo Station", size => DodoStationModel.Create(size / 2f)));
        _shipModels.Add(("Boulder", size => BoulderModel.Create(size)));
        _shipModels.Add(("Splinter", size => SplinterModel.Create(size)));
        _shipModels.Add(("Rock Hermit", size => RockHermitModel.Create(size)));
        _currentModelIndex = 0;
        _cobraModel = _shipModels[_currentModelIndex].Create(2.4f);
        _world = Matrix.Identity;

        // Create 1x1 white texture for UI elements
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        // Initialize audio for menu sounds
        if (_gameInstance != null)
        {
            if (_gameInstance is GameInstance giAudio)
                giAudio.Audio.Initialize();
        }

        // Apply persisted "draw invisible" setting as initial hidden-edge mode
        _showHiddenEdges = _gameInstance?.DrawInvisible ?? _showHiddenEdges;
    }

    public override void Update(GameTime gameTime)
    {
        if (_gameInstance == null) return;
        var input = _gameInstance.Input;
        _lastControl = _gameInstance.FlightControl.Update(gameTime, input);
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Auto-rotate around Z axis тАФ ship spins flat like a clock hand
        if (!_lastControl.IsPaused)
        {
            _shipRotationY += dt * 0.3f;
        }
        _world = Matrix.CreateRotationY(_shipRotationY);


        // Cycle through ship models with left/right
        if (input.IsKeyPressed(Keys.Right))
        {
            _currentModelIndex = (_currentModelIndex + 1) % _shipModels.Count;
            _cobraModel = _shipModels[_currentModelIndex].Create(2.4f);
        }
        if (input.IsKeyPressed(Keys.Left))
        {
            _currentModelIndex = (_currentModelIndex - 1 + _shipModels.Count) % _shipModels.Count;
            _cobraModel = _shipModels[_currentModelIndex].Create(2.4f);
        }

        // Menu navigation
        if (input.IsKeyPressed(Keys.Up))
        {
            _selectedItem = (_selectedItem - 1 + _menuItems.Length) % _menuItems.Length;
            if (_gameInstance is GameInstance gi)
                gi.Audio.PlayMenuSelect();
        }
        if (input.IsKeyPressed(Keys.Down))
        {
            _selectedItem = (_selectedItem + 1) % _menuItems.Length;
            if (_gameInstance is GameInstance gi)
                gi.Audio.PlayMenuSelect();
        }

        if (input.IsKeyPressed(Keys.Enter))
        {
            HandleSelection();
        }

        // Cycle edges with ] and [
        if (input.IsKeyPressed(Keys.OemCloseBrackets))
        {
            if (_cobraModel.Edges.Count > 0)
                _highlightedEdgeIndex = (_highlightedEdgeIndex + 1) % _cobraModel.Edges.Count;
        }
        if (input.IsKeyPressed(Keys.OemOpenBrackets))
        {
            if (_cobraModel.Edges.Count > 0)
                _highlightedEdgeIndex = (_highlightedEdgeIndex - 1 + _cobraModel.Edges.Count) % _cobraModel.Edges.Count;
        }

        // Toggle hidden edges with I
        if (input.IsKeyPressed(Keys.I))
        {
            _showHiddenEdges = !_showHiddenEdges;
            if (_gameInstance != null)
            {
                _gameInstance.DrawInvisible = _showHiddenEdges;
                Systems.OptionsManager.Save(_gameInstance.DrawWhite, _gameInstance.DrawInvisible, _gameInstance.ResolutionIndex, _gameInstance.IsFullScreen);
            }
        }
    }

    private void HandleSelection()
    {
        switch (_selectedItem)
        {
            case 0: // Combat Rating
                int idx = Array.IndexOf(_ratings, _currentRating);
                _currentRating = _ratings[(idx + 1) % _ratings.Length];
                break;
            case 1: // Start New Game → FlightScene
                _gameInstance?.ChangeScene(new FlightScene(_gameInstance));
                break;
            case 2: // Load Game
                if (_gameInstance != null && _hasSavedGame)
                {
                    var savePath = SaveGameManager.GetDefaultSavePath();
                    if (_gameInstance is GameInstance gi2)
                    {
                        if (SaveGameManager.TryLoad(savePath, gi2, out int galaxy, out int system, out var seed))
                        {
                            // TODO: pass galaxy/system context to FlightScene for proper initialization
                            gi2.ChangeScene(new FlightScene(gi2, seed));
                        }
                    }
                }
                break;
            case 3: // Space View → SpaceScene (visual test)
                _gameInstance?.ChangeScene(new SpaceScene(_gameInstance));
                break;
            case 4: // Galaxy Map
                _gameInstance?.PushScene(new GalaxyMapScene(_gameInstance));
                break;
            case 6: // Options
                _gameInstance?.PushScene(new OptionsScene(_gameInstance));
                break;
            case 7: // Quit
                _gameInstance?.Exit();
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw wireframe ship - takes up top 2/3 of screen
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _wireframeRenderer.Draw(_cobraModel, _world, _view, _projection, spriteBatch, useBackFaceCulling: true, _highlightedEdgeIndex, _showHiddenEdges);
        spriteBatch.End();

        // Draw UI overlay - Elite-style left sidebar menu
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        int displayW = _gameInstance?.DisplayWidth ?? 1024;
        int displayH = _gameInstance?.DisplayHeight ?? 768;

        // Left sidebar — 30% of width, full height, clamped to a sensible minimum
        int sidebarW = Math.Max(300, (int)(displayW * 0.3f));
        var sidebarRect = new Rectangle(0, 0, sidebarW, displayH);
        spriteBatch.Draw(_whitePixel, sidebarRect, new Color(10, 10, 30));

        // Ship name at top of sidebar
        _font.DrawString(spriteBatch, _shipModels[_currentModelIndex].Name, new Vector2(20, 15), Color.Cyan, 1.2f);

        // Edge info
        if (_highlightedEdgeIndex >= 0)
            _font.DrawString(spriteBatch, $"EDGE: {_highlightedEdgeIndex}/{_cobraModel.Edges.Count - 1}", new Vector2(20, 40), Color.Red, 0.9f);

        // Hidden edges toggle
        _font.DrawString(spriteBatch, _showHiddenEdges ? "HIDDEN: ON" : "HIDDEN: OFF", new Vector2(20, 60), Color.White, 0.8f);

        // Separator line
        spriteBatch.Draw(_whitePixel, new Rectangle(10, 85, sidebarW - 20, 2), Color.DarkCyan);

        // Combat rating display (prominent, in sidebar)
        _font.DrawString(spriteBatch, "RATING", new Vector2(20, 100), Color.Gray, 0.8f);
        _font.DrawString(spriteBatch, _currentRating, new Vector2(20, 120), Color.Yellow, 1.5f);

        // Separator
        spriteBatch.Draw(_whitePixel, new Rectangle(10, 165, sidebarW - 20, 2), Color.DarkCyan);

        // Menu items - left sidebar, vertical list
        int menuStartY = 185;
        for (int i = 0; i < _menuItems.Length; i++)
        {
            var pos = new Vector2(30, menuStartY + i * 36);
            bool isLoadWithoutSave = (i == 2 && !_hasSavedGame);
            var color = isLoadWithoutSave ? Color.DarkGray : (i == _selectedItem ? Color.Yellow : Color.White);
            var prefix = i == _selectedItem ? "> " : "  ";
            string label = prefix + _menuItems[i];
            if (isLoadWithoutSave) label += " (no save)";
            _font.DrawString(spriteBatch, label, pos, color, 1.1f);
        }

        // Instructions at bottom of sidebar
        spriteBatch.Draw(_whitePixel, new Rectangle(10, displayH - 73, sidebarW - 20, 2), Color.DarkCyan);
        _font.DrawString(spriteBatch, "UP/DOWN: SELECT", new Vector2(20, displayH - 58), Color.Gray, 0.75f);
        _font.DrawString(spriteBatch, "ENTER: CHOOSE", new Vector2(20, displayH - 40), Color.Gray, 0.75f);
        _font.DrawString(spriteBatch, "LEFT/RIGHT: SHIP", new Vector2(20, displayH - 22), Color.Gray, 0.75f);

        // Right-side info panel — anchored just past the sidebar
        _font.DrawString(spriteBatch, "SPACE: PAUSE   ESC: QUIT   [/]: EDGE   [I]: HIDDEN EDGES",
            new Vector2(sidebarW + 20, displayH - 28), Color.Gray, 0.75f);

        if (_lastControl.IsPaused)
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(sidebarW + 100, displayH / 2f), Color.Red, 2f);

        // Debug rotation info when paused
        if (_lastControl.IsPaused)
        {
            var worldToLocal = Matrix.Invert(_world);
            Vector3 shipToCameraLocal = Vector3.TransformNormal(_view.Translation - _world.Translation, worldToLocal);
            _font.DrawString(spriteBatch, $"Cam local: ({shipToCameraLocal.X:F3},{shipToCameraLocal.Y:F3},{shipToCameraLocal.Z:F3})", new Vector2(10, 65), Color.Magenta, 0.8f);
            _font.DrawString(spriteBatch, $"World rot: {_shipRotationY:F3} rad", new Vector2(10, 80), Color.Magenta, 0.8f);
        }

        spriteBatch.End();
    }

    public override void UnloadContent() { }
}
