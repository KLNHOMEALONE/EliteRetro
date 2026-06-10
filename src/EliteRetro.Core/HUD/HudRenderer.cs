using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.HUD;

/// <summary>
/// Authentic Elite dashboard renderer, matched against the Legend reference image.
/// Layout: left ~22% (6 gauges + missile row) | center ~56% (scanner) | right ~22% (SP/RL/DC + 4 energy banks).
/// Style: silver fill on bright-red bar background (fill inset vertically so red edges show),
/// label boxes are full-slot-height black boxes with silver borders stacked flush in a column,
/// silver text auto-scaled to fit inside its box. Everything sits directly on black.
/// </summary>
public class HudRenderer
{
    /// <summary>Fraction of the HUD width used by each side column (left and right).</summary>
    public const float SideFraction = 0.22f;

    private readonly Texture2D _whitePixel;

    private static readonly Color Silver = new Color(200, 200, 200);
    private static readonly Color BarRed = new Color(200, 8, 8);
    private static readonly Color ArmedGreen = new Color(0, 210, 0);
    private static readonly Color ScoopYellow = new Color(225, 195, 0);
    private static readonly Color MissileRed = new Color(220, 30, 30);

    public HudRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, HUDState state, BitmapFont font, Rectangle hudRect, Rectangle screenRect)
    {
        spriteBatch.Draw(_whitePixel, hudRect, Color.Black);

        int sideW = (int)MathF.Round(hudRect.Width * SideFraction);
        var leftRect = new Rectangle(hudRect.X, hudRect.Y, sideW, hudRect.Height);
        var rightRect = new Rectangle(hudRect.Right - sideW, hudRect.Y, sideW, hudRect.Height);

        DrawLeftColumn(spriteBatch, state, font, leftRect);
        DrawRightColumn(spriteBatch, state, font, rightRect);
    }

    /// <summary>
    /// Draw indicators that sit ON TOP of the scanner area (status bulb, scoop icon,
    /// small compass, armed-missile indicator).
    /// </summary>
    public void DrawCenterOverlays(SpriteBatch spriteBatch, HUDState state, Rectangle hudRect)
    {
        int sideW = (int)MathF.Round(hudRect.Width * SideFraction);
        var c = new Rectangle(hudRect.X + sideW, hudRect.Y, hudRect.Width - sideW * 2, hudRect.Height);

        DrawStatusBulb(spriteBatch, state, c);
        DrawSmallCompass(spriteBatch, state.TargetBearing, c);
        if (state.TargetLocked && state.Missiles > 0)
            DrawArmedMissileIndicator(spriteBatch, c);
    }

    // ------------------------------------------------------------------
    // Columns
    // ------------------------------------------------------------------

    private void DrawLeftColumn(SpriteBatch spriteBatch, HUDState state, BitmapFont font, Rectangle rect)
    {
        int slotH = rect.Height / 7;
        int labelW = (int)(rect.Width * 0.26f);
        int gap = Math.Max(3, rect.Width / 50);
        int barX = rect.X + labelW + gap;
        int barW = rect.Right - barX;
        int barH = (int)(slotH * 0.80f);

        // Six gauges: label box on the left, bar on the right.
        (string label, float ratio)[] rows =
        {
            ("FS", state.ShieldForward / 255f),
            ("AS", state.ShieldAft / 255f),
            ("FU", state.Fuel / 70f),
            ("CT", state.CabinTemp / 255f),
            ("LT", state.LaserTemp / 255f),
            ("AL", state.Altitude / 255f),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            int rowY = rect.Y + i * slotH;
            DrawLabelBox(spriteBatch, font, rows[i].label, new Rectangle(rect.X, rowY, labelW, slotH));
            DrawGaugeBar(spriteBatch, new Rectangle(barX, rowY + (slotH - barH) / 2, barW, barH), rows[i].ratio);
        }

        // Slot 7: missile icon label + missile bar with lock square.
        int mRowY = rect.Y + 6 * slotH;
        DrawMissileLabelBox(spriteBatch, new Rectangle(rect.X, mRowY, labelW, slotH));
        DrawMissileBar(spriteBatch, new Rectangle(barX, mRowY + (slotH - barH) / 2, barW, barH), state.Missiles, state.TargetLocked);
    }

    private void DrawRightColumn(SpriteBatch spriteBatch, HUDState state, BitmapFont font, Rectangle rect)
    {
        int slotH = rect.Height / 7;
        int labelW = (int)(rect.Width * 0.26f);
        int gap = Math.Max(3, rect.Width / 50);
        int labelX = rect.Right - labelW;
        int barX = rect.X;
        int barW = labelX - gap - barX;
        int barH = (int)(slotH * 0.80f);

        int rowY = rect.Y;
        var barRect = new Rectangle(barX, rowY + (slotH - barH) / 2, barW, barH);

        // SP: speed gauge
        DrawGaugeBar(spriteBatch, barRect, state.Speed / GameConstants.SpeedMax);
        DrawLabelBox(spriteBatch, font, "SP", new Rectangle(labelX, rowY, labelW, slotH));
        rowY += slotH;

        // RL: roll center indicator
        DrawCenterIndicatorBar(spriteBatch, new Rectangle(barX, rowY + (slotH - barH) / 2, barW, barH), (state.Roll + 1f) / 2f);
        DrawLabelBox(spriteBatch, font, "RL", new Rectangle(labelX, rowY, labelW, slotH));
        rowY += slotH;

        // DC: pitch (dive/climb) center indicator
        DrawCenterIndicatorBar(spriteBatch, new Rectangle(barX, rowY + (slotH - barH) / 2, barW, barH), (state.Pitch + 1f) / 2f);
        DrawLabelBox(spriteBatch, font, "DC", new Rectangle(labelX, rowY, labelW, slotH));
        rowY += slotH;

        // 1-4: energy banks (4 banks per bar)
        for (int i = 1; i <= 4; i++)
        {
            float unitBanks = MathHelper.Clamp(state.EnergyBanks - (i - 1) * 4, 0, 4);
            DrawGaugeBar(spriteBatch, new Rectangle(barX, rowY + (slotH - barH) / 2, barW, barH), unitBanks / 4f);
            DrawLabelBox(spriteBatch, font, i.ToString(), new Rectangle(labelX, rowY, labelW, slotH));
            rowY += slotH;
        }
    }

    // ------------------------------------------------------------------
    // Bars
    // ------------------------------------------------------------------

    /// <summary>
    /// Gauge bar: bright-red background with a silver fill from the left.
    /// The fill is inset vertically so thin red edges show above and below it.
    /// </summary>
    private void DrawGaugeBar(SpriteBatch spriteBatch, Rectangle bar, float ratio)
    {
        spriteBatch.Draw(_whitePixel, bar, BarRed);
        int pad = Math.Max(2, bar.Height / 7);
        int fillW = (int)(MathHelper.Clamp(ratio, 0f, 1f) * bar.Width);
        if (fillW > 0)
            spriteBatch.Draw(_whitePixel, new Rectangle(bar.X, bar.Y + pad, fillW, bar.Height - pad * 2), Silver);
    }

    /// <summary>
    /// RL/DC style bar: red background with a small floating silver indicator
    /// that travels left/right from center. value: 0 = far left, 1 = far right.
    /// </summary>
    private void DrawCenterIndicatorBar(SpriteBatch spriteBatch, Rectangle bar, float value)
    {
        spriteBatch.Draw(_whitePixel, bar, BarRed);

        int indW = Math.Max(3, bar.Width / 26);
        int indH = (int)(bar.Height * 0.68f);
        float travel = bar.Width / 2f - indW;
        int indX = bar.X + bar.Width / 2 - indW / 2 + (int)((MathHelper.Clamp(value, 0f, 1f) - 0.5f) * 2f * travel);
        int indY = bar.Y + (bar.Height - indH) / 2;
        spriteBatch.Draw(_whitePixel, new Rectangle(indX, indY, indW, indH), Silver);
    }

    /// <summary>
    /// Missile bar: silver-outlined rectangle, red area with silver rocket glyphs,
    /// black lock square at the right end separated by a silver divider.
    /// Lock square turns white and rockets turn yellow when a target is locked.
    /// </summary>
    private void DrawMissileBar(SpriteBatch spriteBatch, Rectangle bar, int count, bool locked)
    {
        int border = Math.Max(2, bar.Height / 11);
        int lockW = bar.Height; // roughly square

        var redRect = new Rectangle(bar.X, bar.Y, bar.Width - lockW, bar.Height);
        var lockRect = new Rectangle(bar.Right - lockW, bar.Y, lockW, bar.Height);

        spriteBatch.Draw(_whitePixel, redRect, BarRed);
        spriteBatch.Draw(_whitePixel, lockRect, locked ? Color.White : Color.Black);

        // Outline around the whole bar + divider before the lock square
        DrawRectOutline(spriteBatch, bar, Silver, border);
        spriteBatch.Draw(_whitePixel, new Rectangle(lockRect.X - border, bar.Y, border, bar.Height), Silver);

        // Rocket glyphs
        int gh = bar.Height - border * 2 - 2;
        int gw = Math.Max(4, (int)(gh * 0.75f));
        int spacing = Math.Max(4, gw / 2);
        Color glyphColor = locked ? Color.Yellow : Silver;

        int gx = bar.X + border + spacing;
        int gy = bar.Y + border + 1;
        for (int i = 0; i < Math.Min(count, 4); i++)
        {
            if (gx + gw > lockRect.X - border - 2) break;
            DrawRocketGlyph(spriteBatch, gx, gy, gw, gh, glyphColor);
            gx += gw + spacing;
        }
    }

    /// <summary>
    /// Small upright rocket silhouette like the Legend reference: narrow head,
    /// wider body, flared skirt, and two detached feet at the bottom.
    /// </summary>
    private void DrawRocketGlyph(SpriteBatch spriteBatch, int x, int y, int w, int h, Color color)
    {
        int cx = x + w / 2;
        int feetH = Math.Max(2, h / 5);
        int bodyBottom = y + h - feetH - 1; // 1px gap above the feet
        int headH = Math.Max(2, (int)(h * 0.28f));

        // Head (narrow)
        int headW = Math.Max(2, (int)(w * 0.34f));
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - headW / 2, y, headW, headH), color);

        // Body (wider)
        int bodyW = Math.Max(headW + 2, (int)(w * 0.62f));
        spriteBatch.Draw(_whitePixel, new Rectangle(cx - bodyW / 2, y + headH, bodyW, bodyBottom - (y + headH)), color);

        // Flared skirt at the bottom of the body (full glyph width)
        int skirtH = Math.Max(2, (bodyBottom - (y + headH)) / 3);
        spriteBatch.Draw(_whitePixel, new Rectangle(x, bodyBottom - skirtH, w, skirtH), color);

        // Two detached feet with a center gap
        int footW = Math.Max(2, (int)(w * 0.28f));
        spriteBatch.Draw(_whitePixel, new Rectangle(x, y + h - feetH, footW, feetH), color);
        spriteBatch.Draw(_whitePixel, new Rectangle(x + w - footW, y + h - feetH, footW, feetH), color);
    }

    // ------------------------------------------------------------------
    // Label boxes
    // ------------------------------------------------------------------

    /// <summary>
    /// Full-slot-height black box with a silver border and silver text.
    /// Text is auto-scaled to always fit inside the box.
    /// </summary>
    private void DrawLabelBox(SpriteBatch spriteBatch, BitmapFont font, string label, Rectangle box)
    {
        int border = Math.Max(2, box.Height / 12);
        spriteBatch.Draw(_whitePixel, box, Color.Black);
        DrawRectOutline(spriteBatch, box, Silver, border);

        if (string.IsNullOrEmpty(label)) return;

        var lsz = font.MeasureString(label);
        float maxW = box.Width - border * 2 - 6;
        float maxH = box.Height - border * 2 - 4;
        // +2 accounts for the 1px overdraw of DrawThickText
        float scale = MathF.Min(maxW / MathF.Max(1f, lsz.X + 2), maxH / MathF.Max(1f, lsz.Y + 2));
        scale = MathF.Min(scale, 3f);

        var pos = new Vector2(
            box.X + (box.Width - lsz.X * scale) / 2f,
            box.Y + (box.Height - lsz.Y * scale) / 2f);
        DrawThickText(spriteBatch, font, label, pos, Silver, scale);
    }

    /// <summary>
    /// Label box for the missile row: silver horizontal missile icon
    /// (two fins on the left, pointed tip on the right).
    /// </summary>
    private void DrawMissileLabelBox(SpriteBatch spriteBatch, Rectangle box)
    {
        int border = Math.Max(2, box.Height / 12);
        spriteBatch.Draw(_whitePixel, box, Color.Black);
        DrawRectOutline(spriteBatch, box, Silver, border);

        int ix = box.X + border + 3;
        int iw = box.Width - border * 2 - 8;
        int ih = Math.Max(6, (int)(box.Height * 0.45f));
        int iy = box.Y + (box.Height - ih) / 2;

        int finW = Math.Max(2, iw / 8);
        int finH = Math.Max(2, ih / 3);

        // Two fins at the left (top and bottom)
        spriteBatch.Draw(_whitePixel, new Rectangle(ix, iy, finW, finH), Silver);
        spriteBatch.Draw(_whitePixel, new Rectangle(ix, iy + ih - finH, finW, finH), Silver);

        // Body
        int bodyX = ix + finW + 1;
        int tipW = Math.Max(3, iw / 5);
        int bodyW = Math.Max(2, iw - finW - 1 - tipW);
        spriteBatch.Draw(_whitePixel, new Rectangle(bodyX, iy, bodyW, ih), Silver);

        // Pointed tip (right)
        for (int col = 0; col < tipW; col++)
        {
            float t = 1f - (col + 1) / (float)tipW;
            int hh = Math.Max(1, (int)(ih * t));
            spriteBatch.Draw(_whitePixel, new Rectangle(bodyX + bodyW + col, iy + (ih - hh) / 2, 1, hh), Silver);
        }
    }

    // ------------------------------------------------------------------
    // Center panel overlays
    // ------------------------------------------------------------------

    private void DrawStatusBulb(SpriteBatch spriteBatch, HUDState state, Rectangle c)
    {
        int r = Math.Max(6, (int)(c.Height * 0.11f));
        int edgeGap = Math.Max(3, c.Width / 100);
        int cx = c.X + edgeGap + r;
        int cy = c.Y + (int)(c.Height * 0.16f);

        // Status bulb: green normally, yellow when the station is in view
        Color statusColor = state.StationInView ? Color.Yellow : Color.Lime;
        DrawCircleFilled(spriteBatch, cx, cy, r, statusColor);

        // Fuel scoop / docking computer: small yellow canister below the bulb
        if (state.HasFuelScoop || state.HasDockingComputer)
        {
            int w = (int)(r * 1.7f);
            int h = Math.Max(6, r);
            int x = cx - w / 2;
            int y = cy + r + Math.Max(3, r / 4);

            int lidH = Math.Max(2, (int)(h * 0.35f));
            // Lid (full width)
            spriteBatch.Draw(_whitePixel, new Rectangle(x, y, w, lidH), ScoopYellow);
            // Body (narrower)
            int bodyW = (int)(w * 0.72f);
            spriteBatch.Draw(_whitePixel, new Rectangle(cx - bodyW / 2, y + lidH, bodyW, h - lidH), ScoopYellow);
            // Two dark notches at the bottom corners of the body
            int notch = Math.Max(2, h / 4);
            spriteBatch.Draw(_whitePixel, new Rectangle(cx - bodyW / 2, y + h - notch, notch, notch), Color.Black);
            spriteBatch.Draw(_whitePixel, new Rectangle(cx + bodyW / 2 - notch, y + h - notch, notch, notch), Color.Black);
        }
    }

    private void DrawSmallCompass(SpriteBatch spriteBatch, Vector2 bearing, Rectangle c)
    {
        int r = Math.Max(8, (int)(c.Height * 0.16f));
        int edgeGap = Math.Max(3, c.Width / 100);
        int cx = c.Right - edgeGap - r;
        int cy = c.Y + (int)(c.Height * 0.22f);

        // Dotted circle outline (like the scanner ellipse style)
        int dots = Math.Max(16, (int)(MathF.Tau * r / 5f));
        int dotSize = Math.Max(2, r / 12);
        for (int i = 0; i < dots; i++)
        {
            float a = (i / (float)dots) * MathF.Tau;
            int dx = cx + (int)(MathF.Cos(a) * r);
            int dy = cy + (int)(MathF.Sin(a) * r);
            spriteBatch.Draw(_whitePixel, new Rectangle(dx - dotSize / 2, dy - dotSize / 2, dotSize, dotSize), Silver);
        }

        // Target dot: bearing is -1..1 on both axes, clamped to the circle
        Vector2 clamped = bearing;
        float dist = bearing.Length();
        if (dist > 1f) clamped /= dist;

        int targetSize = Math.Max(3, r / 4);
        int tx = cx + (int)(clamped.X * (r - targetSize));
        int ty = cy + (int)(clamped.Y * (r - targetSize));
        spriteBatch.Draw(_whitePixel, new Rectangle(tx - targetSize / 2, ty - targetSize / 2, targetSize, targetSize), Color.White);
    }

    /// <summary>
    /// Green missile icon at mid-right of the scanner panel — shown when a missile is armed/locked.
    /// </summary>
    private void DrawArmedMissileIndicator(SpriteBatch spriteBatch, Rectangle c)
    {
        int w = Math.Max(16, (int)(c.Width * 0.064f));
        int h = Math.Max(8, (int)(c.Height * 0.085f));
        int edgeGap = Math.Max(3, c.Width / 100);
        int x = c.Right - edgeGap - (int)(c.Height * 0.16f) - w / 2; // aligned under the compass
        int y = c.Y + (int)(c.Height * 0.635f) - h / 2;

        int finW = Math.Max(2, w / 8);
        int finH = Math.Max(2, h / 3);
        int tipW = Math.Max(3, w / 5);

        // Fins at the left
        spriteBatch.Draw(_whitePixel, new Rectangle(x, y, finW, finH), ArmedGreen);
        spriteBatch.Draw(_whitePixel, new Rectangle(x, y + h - finH, finW, finH), ArmedGreen);
        // Body
        int bodyX = x + finW + 1;
        int bodyW = Math.Max(2, w - finW - 1 - tipW);
        spriteBatch.Draw(_whitePixel, new Rectangle(bodyX, y, bodyW, h), ArmedGreen);
        // Pointed tip (right)
        for (int col = 0; col < tipW; col++)
        {
            float t = 1f - (col + 1) / (float)tipW;
            int hh = Math.Max(1, (int)(h * t));
            spriteBatch.Draw(_whitePixel, new Rectangle(bodyX + bodyW + col, y + (h - hh) / 2, 1, hh), ArmedGreen);
        }
    }

    // ------------------------------------------------------------------
    // Primitives
    // ------------------------------------------------------------------

    private void DrawRectOutline(SpriteBatch spriteBatch, Rectangle r, Color color, int thickness = 1)
    {
        thickness = Math.Max(1, thickness);
        spriteBatch.Draw(_whitePixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
        spriteBatch.Draw(_whitePixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
        spriteBatch.Draw(_whitePixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        spriteBatch.Draw(_whitePixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
    }

    private void DrawCircleFilled(SpriteBatch spriteBatch, int cx, int cy, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            int x = (int)MathF.Sqrt(radius * radius - y * y);
            spriteBatch.Draw(_whitePixel, new Rectangle(cx - x, cy + y, x * 2, 1), color);
        }
    }

    private static void DrawThickText(SpriteBatch spriteBatch, BitmapFont font, string text, Vector2 pos, Color color, float scale)
    {
        // Bitmap font has no bold; emulate thickness with 1px overdraw.
        font.DrawString(spriteBatch, text, pos, color, scale);
        font.DrawString(spriteBatch, text, pos + new Vector2(1, 0), color, scale);
        font.DrawString(spriteBatch, text, pos + new Vector2(0, 1), color, scale);
    }
}
