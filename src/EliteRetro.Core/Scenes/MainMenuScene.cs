using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteRetro.Core.Entities;
using EliteRetro.Core.Rendering;

namespace EliteRetro.Core.Scenes;

public class MainMenuScene : GameScene
{
    private WireframeRenderer _wireframeRenderer = null!;
    private ShipModel _cobraModel = null!;
    private BitmapFont _font = null!;
    private Game? _game;
    private Matrix _world;
    private Matrix _view;
    private Matrix _projection;
    private GraphicsDevice? _graphicsDevice;
    private int _selectedItem;
    private readonly string[] _menuItems = {
        "COMBAT RATING",
        "START NEW GAME",
        "LOAD COMMANDER",
        "TOP PILOTS",
        "OPTIONS",
        "QUIT"
    };
    private readonly string[] _ratings = { "HARMLESS", "MOSTLY HARMLESS", "NOVICE", "COMPETENT", "EXPERT", "DANGEROUS", "DEADLY", "ELITE" };
    private string _currentRating = "DANGEROUS";
    private KeyboardState _prevKb;
    private float _shipRotationY;
    private int _currentModelIndex;
    private bool _paused;
    private readonly List<(string Name, Func<float, ShipModel> Create)> _shipModels = new();

    public MainMenuScene(Game? game = null)
    {
        _game = game;
    }

    public override void LoadContent(ContentManager content, BitmapFont font)
    {
        _font = font;
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
        _shipModels.Add(("Anaconda", size => AnacondaModel.Create(size)));
        _shipModels.Add(("Fer-de-Lance", size => FerDeLanceModel.Create(size)));
        _currentModelIndex = 0;
        _cobraModel = _shipModels[_currentModelIndex].Create(2.4f);
        _world = Matrix.Identity;
    }

    private void EnsureInitialized(SpriteBatch spriteBatch)
    {
        if (_graphicsDevice != null) return;

        _graphicsDevice = spriteBatch.GraphicsDevice;
        _wireframeRenderer = new WireframeRenderer(_graphicsDevice);
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            _graphicsDevice.Viewport.AspectRatio,
            0.1f, 1000f);
        // Camera positioned to show ship in top 2/3
        _view = Matrix.CreateLookAt(new Vector3(0, 10f, 6), new Vector3(0, -1f, 0), Vector3.Up);
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Toggle pause on Space
        if (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space))
        {
            _paused = !_paused;
        }

        // Auto-rotate around Z axis — ship spins flat like a clock hand
        if (!_paused)
        {
            _shipRotationY += dt * 0.3f;
        }
        _world = Matrix.CreateRotationX(-MathHelper.PiOver2) *  // Point ship up (+Y)
                 Matrix.CreateRotationZ(_shipRotationY);

        // Cycle through ship models with left/right
        if (kb.IsKeyDown(Keys.Right) && _prevKb.IsKeyUp(Keys.Right))
        {
            _currentModelIndex = (_currentModelIndex + 1) % _shipModels.Count;
            _cobraModel = _shipModels[_currentModelIndex].Create(2.4f);
        }
        if (kb.IsKeyDown(Keys.Left) && _prevKb.IsKeyUp(Keys.Left))
        {
            _currentModelIndex = (_currentModelIndex - 1 + _shipModels.Count) % _shipModels.Count;
            _cobraModel = _shipModels[_currentModelIndex].Create(2.4f);
        }

        // Menu navigation
        if (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up))
            _selectedItem = (_selectedItem - 1 + _menuItems.Length) % _menuItems.Length;
        if (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down))
            _selectedItem = (_selectedItem + 1) % _menuItems.Length;

        if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
        {
            HandleSelection();
        }

        if (kb.IsKeyDown(Keys.Escape))
        {
            _game?.Exit();
        }

        _prevKb = kb;
    }

    private void HandleSelection()
    {
        switch (_selectedItem)
        {
            case 0: // Combat Rating
                int idx = Array.IndexOf(_ratings, _currentRating);
                _currentRating = _ratings[(idx + 1) % _ratings.Length];
                break;
            case 1: // Start New Game
                if (_game is GameInstance gi)
                    gi.ChangeScene(new SpaceScene());
                break;
            case 2: // Load Commander
                if (_game is GameInstance gi2)
                    gi2.ChangeScene(new GalaxyMapScene());
                break;
            case 5: // Quit
                _game?.Exit();
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        EnsureInitialized(spriteBatch);

        // Draw wireframe ship - takes up top 2/3 of screen
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _wireframeRenderer.Draw(_cobraModel, _world, _view, _projection, spriteBatch, useBackFaceCulling: false);
        spriteBatch.End();

        // Draw UI overlay - menu items in bottom 1/3
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Ship name in top-left corner
        _font.DrawString(spriteBatch, _shipModels[_currentModelIndex].Name, new Vector2(10, 10), Color.Cyan, 1f);

        // Menu items - centered horizontally, in bottom third
        int menuStartY = 540;
        for (int i = 0; i < _menuItems.Length; i++)
        {
            var pos = new Vector2(380, menuStartY + i * 32);
            var color = i == _selectedItem ? Color.Yellow : Color.Lime;
            var prefix = i == _selectedItem ? "* " : "  ";
            _font.DrawString(spriteBatch, prefix + _menuItems[i], pos, color, 1.2f);
        }

        // Current rating display
        _font.DrawString(spriteBatch, $"RATING: {_currentRating}", new Vector2(380, 520), Color.Yellow, 1f);

        // Instructions at very bottom
        _font.DrawString(spriteBatch, "UP/DOWN: SELECT   ENTER: CHOOSE   ESC: QUIT   SPACE: PAUSE", new Vector2(220, 750), Color.Gray, 0.8f);

        if (_paused)
            _font.DrawString(spriteBatch, "PAUSED", new Vector2(380, 500), Color.Red, 1.5f);

        spriteBatch.End();
    }

    public override void UnloadContent() { }
}
