using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using EliteRetro.Core.Systems;

namespace EliteRetro.Core.Scenes;

/// <summary>
/// Full-screen star system data (BBC Elite style). Opened from <see cref="GalaxyMapScene"/> with I.
/// </summary>
public class GalaxyStarDescriptionScene : GameScene
{
    private readonly StarSystem _system;
    private readonly float _distanceLightYears;
    private BitmapFont _font = null!;
    private Texture2D _whitePixel = null!;
    private int _screenW;
    private int _screenH;

    public GalaxyStarDescriptionScene(StarSystem system, float distanceLightYears)
    {
        _system = system;
        _distanceLightYears = distanceLightYears;
    }

    public override void LoadContent(ContentManager content, BitmapFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _screenW = graphicsDevice.Viewport.Width;
        _screenH = graphicsDevice.Viewport.Height;
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public override void Update(GameTime gameTime)
    {
        // Escape handled by SceneManager — pops back to galaxy map
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, _screenW, _screenH), Color.Black);

        int margin = Math.Clamp(Math.Min(_screenW, _screenH) / 16, 24, 64);
        var panel = new Rectangle(margin, margin, _screenW - margin * 2, _screenH - margin * 2);
        DrawRectOutline(spriteBatch, panel, Color.White);

        var cyan = new Color(0, 255, 255);
        int labelCol = panel.X + 24;
        int y = panel.Y + 24;
        const float line = 1.1f;
        int valueCol = labelCol + ComputeValueColumnX(scale: line);
        int valueWrapWidthPx = panel.Right - valueCol - 24;

        string name = string.IsNullOrEmpty(_system.Name) ? "UNKNOWN" : _system.Name.ToUpperInvariant();
        _font.DrawString(spriteBatch, $"DATA ON {name}", new Vector2(labelCol, y), cyan, 1.4f);
        y += 44;

        DrawPair(spriteBatch, labelCol, valueCol, valueWrapWidthPx, "Distance:", $"{_distanceLightYears:F1} Light Years", ref y, cyan, line);
        DrawPair(spriteBatch, labelCol, valueCol, valueWrapWidthPx, "Economy:", EconomyDisplay(_system.Economy), ref y, cyan, line);
        DrawPair(spriteBatch, labelCol, valueCol, valueWrapWidthPx, "Government:", GovernmentDisplay(_system.Government), ref y, cyan, line);
        DrawPair(spriteBatch, labelCol, valueCol, valueWrapWidthPx, "Tech.Level:", _system.TechLevel.ToString(), ref y, cyan, line);

        float popBillions = _system.Population * 0.1f;
        string popLine = $"{popBillions:F1} Billion ({_system.Inhabitants})";
        DrawPair(spriteBatch, labelCol, valueCol, valueWrapWidthPx, "Population:", popLine, ref y, cyan, line);

        string prod = $"{_system.GrossProductivity:N0} M CR";
        DrawPair(spriteBatch, labelCol, valueCol, valueWrapWidthPx, "Gross Productivity:", prod, ref y, cyan, line);
        DrawPair(spriteBatch, labelCol, valueCol, valueWrapWidthPx, "Average Radius:", $"{_system.Radius} km", ref y, cyan, line);

        y += 16;
        int wrapWidthPx = panel.Right - labelCol - 24;
        foreach (var lineText in WrapText(_system.FlavourText, wrapWidthPx, line))
        {
            _font.DrawString(spriteBatch, lineText, new Vector2(labelCol, y), Color.White, line);
            y += 22;
        }

        int footerY = panel.Bottom - 40;
        _font.DrawString(spriteBatch, "ESC: RETURN TO CHART", new Vector2(labelCol, footerY), Color.Gray, 0.95f);

        spriteBatch.End();
    }

    private void DrawPair(SpriteBatch sb, int labelX, int valueX, int valueWrapWidthPx, string label, string value, ref int y, Color labelColor, float scale)
    {
        _font.DrawString(sb, label, new Vector2(labelX, y), labelColor, scale);

        bool firstLine = true;
        foreach (var vLine in WrapText(value, valueWrapWidthPx, scale))
        {
            if (!firstLine)
                y += 26;
            _font.DrawString(sb, vLine, new Vector2(valueX, y), Color.White, scale);
            firstLine = false;
        }

        y += 26;
    }

    private void DrawRectOutline(SpriteBatch sb, Rectangle r, Color c)
    {
        sb.Draw(_whitePixel, new Rectangle(r.Left, r.Top, r.Width, 1), c);
        sb.Draw(_whitePixel, new Rectangle(r.Left, r.Bottom - 1, r.Width, 1), c);
        sb.Draw(_whitePixel, new Rectangle(r.Left, r.Top, 1, r.Height), c);
        sb.Draw(_whitePixel, new Rectangle(r.Right - 1, r.Top, 1, r.Height), c);
    }

    private int ComputeValueColumnX(float scale)
    {
        // Make sure long labels (e.g. "Gross Productivity:") never collide with values.
        // Uses actual font metrics so it stays correct even if the font changes.
        string[] labels =
        {
            "Distance:", "Economy:", "Government:", "Tech.Level:",
            "Population:", "Gross Productivity:", "Average Radius:"
        };

        float maxW = 0;
        for (int i = 0; i < labels.Length; i++)
        {
            float w = _font.MeasureString(labels[i]).X * scale;
            if (w > maxW) maxW = w;
        }

        const int gap = 28;
        return (int)MathF.Ceiling(maxW) + gap;
    }

    private static string EconomyDisplay(EconomyType e) => e switch
    {
        EconomyType.RichIndustrial => "Rich Industrial",
        EconomyType.AverageIndustrial => "Average Industrial",
        EconomyType.PoorIndustrial => "Poor Industrial",
        EconomyType.MainlyIndustrial => "Mainly Industrial",
        EconomyType.MainlyAgricultural => "Mainly Agricultural",
        EconomyType.RichAgricultural => "Rich Agricultural",
        EconomyType.AverageAgricultural => "Average Agricultural",
        EconomyType.PoorAgricultural => "Poor Agricultural",
        _ => e.ToString()
    };

    private static string GovernmentDisplay(GovernmentType g) => g switch
    {
        GovernmentType.MultiGov => "Multi-Government",
        GovernmentType.CorpState => "Corporate State",
        _ => g.ToString()
    };

    private IEnumerable<string> WrapText(string text, int maxWidthPixels, float scale)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            if (current.Length == 0)
            {
                current.Append(word);
                continue;
            }

            string candidate = current + " " + word;
            float width = _font.MeasureString(candidate).X * scale;
            if (width <= maxWidthPixels)
            {
                current.Append(' ').Append(word);
                continue;
            }

            // Emit the current line and start a new one.
            yield return current.ToString();
            current.Clear();

            // If a single word is too wide, hard-split it to fit.
            if (_font.MeasureString(word).X * scale <= maxWidthPixels)
            {
                current.Append(word);
                continue;
            }

            int idx = 0;
            while (idx < word.Length)
            {
                int take = 1;
                while (idx + take <= word.Length &&
                       _font.MeasureString(word.Substring(idx, take)).X * scale <= maxWidthPixels)
                {
                    take++;
                }
                take = Math.Max(1, take - 1);

                yield return word.Substring(idx, take);
                idx += take;
            }
        }

        if (current.Length > 0)
            yield return current.ToString();
    }

    public override void UnloadContent()
    {
        _whitePixel?.Dispose();
    }
}
