using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Drawing.Drawing2D;
using Bitmap = System.Drawing.Bitmap;
using Color = Microsoft.Xna.Framework.Color;

namespace EliteRetro.Core;

/// <summary>
/// Runtime bitmap font using GDI+ TrueType rendering.
/// Clean, modern, readable — no pixel art.
/// </summary>
public class BitmapFont
{
    private readonly Texture2D _atlas;
    private readonly Dictionary<char, Microsoft.Xna.Framework.Rectangle> _glyphRects = new();
    private readonly int _lineHeight;
    private const int AtlasW = 1024;
    private const int AtlasH = 256;

    public BitmapFont(GraphicsDevice gd)
    {
        using var font = new System.Drawing.Font("Segoe UI", 20, FontStyle.Regular, GraphicsUnit.Pixel);
        _lineHeight = (int)Math.Ceiling(font.GetHeight());

        var renderedGlyphs = new List<(char c, Bitmap bmp, int x, int y)>();
        int x = 2, y = 2, maxRowH = 0;

        for (int ci = 32; ci <= 126; ci++)
        {
            char c = (char)ci;
            using var tmpBmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(tmpBmp);
            var size = g.MeasureString(c.ToString(), font);
            int gw = Math.Max(1, (int)Math.Ceiling(size.Width));
            int gh = Math.Max(1, (int)Math.Ceiling(size.Height));

            // Wrap to next row
            if (x + gw + 2 > AtlasW - 2) { x = 2; y += maxRowH + 2; maxRowH = 0; }

            // Render glyph
            var glyphBmp = new Bitmap(gw + 2, gh + 2);
            using (var glyphG = Graphics.FromImage(glyphBmp))
            {
                glyphG.Clear(System.Drawing.Color.Transparent);
                glyphG.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                glyphG.SmoothingMode = SmoothingMode.AntiAlias;
                glyphG.DrawString(c.ToString(), font, Brushes.White, 1, 1);
            }

            renderedGlyphs.Add((c, glyphBmp, x, y));
            _glyphRects[c] = new Microsoft.Xna.Framework.Rectangle(x, y, gw + 2, gh + 2);
            if (gh + 2 > maxRowH) maxRowH = gh + 2;
            x += gw + 4;
        }

        // Verify atlas fits
        int totalH = y + maxRowH + 2;
        if (totalH > AtlasH)
            throw new InvalidOperationException($"Font atlas overflow: need {totalH}px height, have {AtlasH}");

        // Build texture
        _atlas = new Texture2D(gd, AtlasW, AtlasH);
        var data = new Color[AtlasW * AtlasH];

        foreach (var (c, bmp, gx, gy) in renderedGlyphs)
        {
            for (int py = 0; py < bmp.Height; py++)
            {
                for (int px = 0; px < bmp.Width; px++)
                {
                    var pixel = bmp.GetPixel(px, py);
                    if (pixel.A > 1)
                        data[(gy + py) * AtlasW + gx + px] = Color.White * (pixel.A / 255f);
                }
            }
            bmp.Dispose();
        }

        _atlas.SetData(data);
    }

    public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, float scale = 1f)
    {
        float x = pos.X;
        float y = pos.Y;
        foreach (char c in text)
        {
            if (c == '\n') { x = pos.X; y += _lineHeight * scale; continue; }

            if (!_glyphRects.TryGetValue(c, out var rect))
            {
                x += 12 * scale;
                continue;
            }

            var dst = new Microsoft.Xna.Framework.Rectangle((int)x, (int)y, (int)(rect.Width * scale), (int)(rect.Height * scale));
            sb.Draw(_atlas, dst, rect, color);
            x += rect.Width * scale;
        }
    }

    public int LineHeight => _lineHeight;
}
