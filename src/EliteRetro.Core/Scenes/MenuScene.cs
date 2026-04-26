using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Scenes;

public class MenuScene : GameScene
{
    private BitmapFont _font = null!;
    private int _selectedItem;
    private readonly string[] _menuItems = { "Space View", "Galaxy Map" };
    private Game? _game;
    private KeyboardState _prevKb;

    public MenuScene(Game? game = null)
    {
        _game = game;
    }

    public override void LoadContent(ContentManager content, BitmapFont font)
    {
        _font = font;
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();

        if (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up))
            _selectedItem = (_selectedItem - 1 + _menuItems.Length) % _menuItems.Length;
        if (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down))
            _selectedItem = (_selectedItem + 1) % _menuItems.Length;

        if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
        {
            switch (_selectedItem)
            {
                case 0:
                    if (_game is GameInstance gi)
                        gi.ChangeScene(new SpaceScene());
                    break;
                case 1:
                    if (_game is GameInstance gi2)
                        gi2.PushScene(new GalaxyMapScene());
                    break;
            }
        }

        _prevKb = kb;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin();

        _font.DrawString(spriteBatch, "ELITE RETRO", new Vector2(350, 150), Color.Lime, 2f);

        for (int i = 0; i < _menuItems.Length; i++)
        {
            var pos = new Vector2(400, 300 + i * 30);
            var color = i == _selectedItem ? Color.Yellow : Color.White;
            var prefix = i == _selectedItem ? "> " : "  ";
            _font.DrawString(spriteBatch, prefix + _menuItems[i], pos, color, 1.5f);
        }

        _font.DrawString(spriteBatch, "ESC: Exit", new Vector2(350, 450), Color.Gray, 1f);

        spriteBatch.End();
    }

    public override void UnloadContent() { }
}
