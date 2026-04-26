using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Scenes;

public abstract class GameScene
{
    public abstract void LoadContent(ContentManager content, BitmapFont font);
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(SpriteBatch spriteBatch);
    public abstract void UnloadContent();
}
