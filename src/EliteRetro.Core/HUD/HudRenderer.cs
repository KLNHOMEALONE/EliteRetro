using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.HUD;

/// <summary>
/// Authentic BBC Elite dashboard renderer.
/// Instrument panel occupies bottom ~1/3 of screen.
/// Layout: left 1/4 (7 horizontal bars) | center 2/4 (compass) | right 1/4 (7 horizontal bars).
/// White fill on red background for all bar indicators. White labels on black.
/// </summary>
public class HudRenderer
{
    private readonly Texture2D _whitePixel;

    private const int SW = 1024;
    private const int SH = 768;
    private const int DashY = 480;
    private const int DashH = 288;
    private const int BarSlotH = DashH / 7;

    private const int LeftX = 0;
    private const int LeftW = 256;
    private const int CenterX = 256;
    private const int CenterW = 512;
    private const int RightX = 768;
    private const int RightW = 256;

    private static readonly Color Amber = new Color(255, 180, 50);
    private static readonly Color AmberDim = new Color(100, 70, 20);
    private static readonly Color PanelBg = new Color(8, 8, 12);
    private static readonly Color PanelBorder = new Color(40, 35, 25);
    private static readonly Color LabelBg = Color.Black;
    private static readonly Color LabelText = Color.White;
    private static readonly Color BarFill = Color.White;
    private static readonly Color BarBg = new Color(160, 20, 20); // deep red background

    // Visual tuning: spacing between label box and bar, plus extra padding inside panels.
    private const int LabelBarGap = 10;
    private const int PanelInnerPad = 6;

    public HudRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, HUDState state, BitmapFont font)
    {
        spriteBatch.Draw(_whitePixel, new Rectangle(0, DashY, SW, DashH), PanelBg);
        spriteBatch.Draw(_whitePixel, new Rectangle(0, DashY, SW, 2), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(LeftW - 1, DashY, 2, DashH), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(RightX - 1, DashY, 2, DashH), PanelBorder);

        DrawLeftBars(spriteBatch, state, font);
        DrawCompass(spriteBatch, state.CompassHeading, font);
        DrawRightBars(spriteBatch, state, font);

        if (!string.IsNullOrEmpty(state.ViewMode))
            font.DrawString(spriteBatch, state.ViewMode, new Vector2(10, 10), Amber, 1.5f);

        if (!string.IsNullOrEmpty(state.StatusMessage))
        {
            var size = font.MeasureString(state.StatusMessage);
            font.DrawString(spriteBatch, state.StatusMessage,
                new Vector2((SW - size.X) / 2, 15), state.StatusColor, 1.2f);
        }

        // Combat rank and legal status in top-right corner
        string legalText = state.LegalStatus switch
        {
            0 => "CLEAN",
            < 50 => "OFFENDER",
            _ => "FUGITIVE"
        };
        var legalSz = font.MeasureString(legalText);
        font.DrawString(spriteBatch, legalText,
            new Vector2(SW - legalSz.X - 10, 10),
            state.LegalStatus >= 50 ? Color.OrangeRed : Color.Lime, 1.0f);

        if (!string.IsNullOrEmpty(state.CombatRank))
        {
            var rankSz = font.MeasureString(state.CombatRank);
            font.DrawString(spriteBatch, state.CombatRank,
                new Vector2(SW - rankSz.X - 10, 32),
                Color.Gold, 1.0f);
        }
    }

    private void DrawLeftBars(SpriteBatch spriteBatch, HUDState state, BitmapFont font)
    {
        int barY = DashY + 2;
        int barH = BarSlotH - 4;
        int labelW = 36;
        int barMaxW = LeftW - labelW - (LabelBarGap + PanelInnerPad);

        float fwdRatio = MathHelper.Clamp(state.ShieldForward / 255f, 0, 1);
        DrawBarH(spriteBatch, LeftX + 2, barY, labelW, barH, (int)(fwdRatio * barMaxW), barMaxW, "FS", font);
        barY += BarSlotH;

        float aftRatio = MathHelper.Clamp(state.ShieldAft / 255f, 0, 1);
        DrawBarH(spriteBatch, LeftX + 2, barY, labelW, barH, (int)(aftRatio * barMaxW), barMaxW, "AS", font);
        barY += BarSlotH;

        float fuelRatio = MathHelper.Clamp(state.Fuel / 70f, 0, 1);
        DrawBarH(spriteBatch, LeftX + 2, barY, labelW, barH, (int)(fuelRatio * barMaxW), barMaxW, "FU", font);
        barY += BarSlotH;

        float cabinRatio = MathHelper.Clamp(state.CabinTemp / 255f, 0, 1);
        DrawBarH(spriteBatch, LeftX + 2, barY, labelW, barH, (int)(cabinRatio * barMaxW), barMaxW, "CT", font);
        barY += BarSlotH;

        float laserRatio = MathHelper.Clamp(state.LaserTemp / 255f, 0, 1);
        DrawBarH(spriteBatch, LeftX + 2, barY, labelW, barH, (int)(laserRatio * barMaxW), barMaxW, "LT", font);
        barY += BarSlotH;

        float altRatio = MathHelper.Clamp(state.Altitude / 255f, 0, 1);
        DrawBarH(spriteBatch, LeftX + 2, barY, labelW, barH, (int)(altRatio * barMaxW), barMaxW, "AL", font);
        barY += BarSlotH;

        float bankRatio = MathHelper.Clamp(state.EnergyBanks / 16f, 0, 1);
        DrawBarH(spriteBatch, LeftX + 2, barY, labelW, barH, (int)(bankRatio * barMaxW), barMaxW, "EB", font);

    }

    private void DrawRightBars(SpriteBatch spriteBatch, HUDState state, BitmapFont font)
    {
        int barY = DashY + 2;
        int barH = BarSlotH - 4;
        int labelW = 36;
        int barMaxW = RightW - labelW - (LabelBarGap + PanelInnerPad);

        float speedRatio = MathHelper.Clamp(state.Speed / 40f, 0, 1);
        DrawBarH(spriteBatch, RightX + 2, barY, labelW, barH, (int)(speedRatio * barMaxW), barMaxW, "SP", font, textRight: true, labelOnRight: true);
        barY += BarSlotH;

        float rollNorm = (state.Roll + 1) / 2;
        DrawBarV(spriteBatch, RightX + 2, barY, labelW, barH, rollNorm, "RL", font, textRight: true, labelOnRight: true);
        barY += BarSlotH;

        float pitchNorm = (state.Pitch + 1) / 2;
        DrawBarV(spriteBatch, RightX + 2, barY, labelW, barH, pitchNorm, "DC", font, textRight: true, labelOnRight: true);
        barY += BarSlotH;

        float msRatio = state.MaxMissiles > 0 ? MathHelper.Clamp((float)state.Missiles / state.MaxMissiles, 0, 1) : 0;
        DrawBarH(spriteBatch, RightX + 2, barY, labelW, barH, (int)(msRatio * barMaxW), barMaxW, "1", font, textRight: true, labelOnRight: true);
        barY += BarSlotH;

        DrawBarH(spriteBatch, RightX + 2, barY, labelW, barH, barMaxW, barMaxW, "2", font, textRight: true, labelOnRight: true);
        barY += BarSlotH;

        DrawBarH(spriteBatch, RightX + 2, barY, labelW, barH, barMaxW, barMaxW, "3", font, textRight: true, labelOnRight: true);
        barY += BarSlotH;

        DrawBarH(spriteBatch, RightX + 2, barY, labelW, barH, barMaxW, barMaxW, "4", font, textRight: true, labelOnRight: true);
    }

    /// <summary>
    /// Horizontal bar: white text label on black (left), white fill on red background (right).
    /// textRight: if true, label is right-aligned within labelW area.
    /// </summary>
    private void DrawBarH(
        SpriteBatch spriteBatch,
        int x,
        int y,
        int labelW,
        int h,
        int filledW,
        int maxW,
        string label,
        BitmapFont font,
        bool textRight = false,
        bool labelOnRight = false)
    {
        // Layout: either [label][gap][bar] (default) or [bar][gap][label] (labelOnRight)
        int gap = LabelBarGap;
        int labelX = labelOnRight ? x + maxW + gap : x;
        int barX = labelOnRight ? x : x + labelW + gap;

        // Label background (black) + text (white)
        spriteBatch.Draw(_whitePixel, new Rectangle(labelX, y, labelW, h), LabelBg);
        if (!string.IsNullOrEmpty(label))
        {
            var lsz = font.MeasureString(label);
            float textX = textRight ? labelX + labelW - lsz.X - 2 : labelX + 2;
            DrawThickText(spriteBatch, font, label,
                new Vector2(textX, y + (h - lsz.Y) / 2),
                LabelText, 1.2f);
        }

        // Bar area: red background full height, white fill with visible padding
        int barTopPad = 4;
        int barBotPad = 4;
        spriteBatch.Draw(_whitePixel, new Rectangle(barX, y, maxW, h), BarBg);
        if (filledW > 0)
            spriteBatch.Draw(_whitePixel, new Rectangle(barX, y + barTopPad, filledW, h - barTopPad - barBotPad), BarFill);

        // Border
        spriteBatch.Draw(_whitePixel, new Rectangle(barX - 1, y + 1, 1, h - 2), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(barX + maxW, y + 1, 1, h - 2), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(barX - 1, y + 1, maxW + 2, 1), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(barX - 1, y + h - 2, maxW + 2, 1), PanelBorder);
    }

    /// <summary>
    /// Vertical bar: indicator line in center, moves left/right from center based on value (0=left, 1=right).
    /// Label at top, bar below.
    /// </summary>
    private void DrawBarV(
        SpriteBatch spriteBatch,
        int x,
        int y,
        int labelW,
        int barH,
        float normalizedValue,
        string label,
        BitmapFont font,
        bool textRight = false,
        bool labelOnRight = false)
    {
        int gap = LabelBarGap;
        int barW = RightW - labelW - (LabelBarGap + PanelInnerPad);
        int labelX = labelOnRight ? x + barW + gap : x;
        int barX = labelOnRight ? x : x + labelW + gap;

        spriteBatch.Draw(_whitePixel, new Rectangle(labelX, y, labelW, barH), LabelBg);
        if (!string.IsNullOrEmpty(label))
        {
            var lsz = font.MeasureString(label);
            float textX = textRight ? labelX + labelW - lsz.X - 2 : labelX + 2;
            DrawThickText(spriteBatch, font, label,
                new Vector2(textX, y + (barH - lsz.Y) / 2),
                LabelText, 1.2f);
        }

        spriteBatch.Draw(_whitePixel, new Rectangle(barX, y, barW, barH), BarBg);

        int centerX = barX + barW / 2;
        int indicatorHalfW = 6;
        int indicatorH = barH - 2;
        int indicatorX = centerX - indicatorHalfW;
        int indicatorY = y + 1;

        int offset = (int)((normalizedValue - 0.5f) * (barW / 2 - indicatorHalfW - 2));
        indicatorX += offset;

        spriteBatch.Draw(_whitePixel, new Rectangle(indicatorX, indicatorY, indicatorHalfW * 2, indicatorH), BarFill);

        spriteBatch.Draw(_whitePixel, new Rectangle(barX - 1, y, 1, barH), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(barX + barW, y, 1, barH), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(barX, y, barW, 1), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(barX, y + barH - 1, barW, 1), PanelBorder);

        spriteBatch.Draw(_whitePixel, new Rectangle(centerX - 1, y + 2, 1, barH - 4), AmberDim);
    }

    private static void DrawThickText(SpriteBatch spriteBatch, BitmapFont font, string text, Vector2 pos, Color color, float scale)
    {
        // Bitmap font has no bold; emulate thickness with 1px overdraw.
        font.DrawString(spriteBatch, text, pos, color, scale);
        font.DrawString(spriteBatch, text, pos + new Vector2(1, 0), color, scale);
        font.DrawString(spriteBatch, text, pos + new Vector2(0, 1), color, scale);
    }

    private void DrawCompass(SpriteBatch spriteBatch, float heading, BitmapFont font)
    {
        spriteBatch.Draw(_whitePixel, new Rectangle(CenterX + 1, DashY + 1, CenterW - 2, DashH - 2), new Color(12, 10, 8));

        float twoPi = MathHelper.TwoPi;
        heading = (heading % twoPi + twoPi) % twoPi;

        string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int segW = CenterW / 8;
        int segH = DashH / 8;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int sx = CenterX + 5 + col * segW;
                int sy = DashY + 5 + row * segH;
                Color c = (row + col) % 2 == 0 ? new Color(18, 16, 10) : new Color(22, 20, 12);
                spriteBatch.Draw(_whitePixel, new Rectangle(sx, sy, segW - 1, segH - 1), c);
            }
        }

        for (int i = 0; i < 8; i++)
        {
            int segCx = CenterX + i * segW + segW / 2;
            int segCy = DashY + segH / 2;
            var dsz = font.MeasureString(dirs[i]);
            font.DrawString(spriteBatch, dirs[i],
                new Vector2(segCx - dsz.X / 2, segCy - dsz.Y / 2), AmberDim, 0.7f);
        }

        int midX = CenterX + CenterW / 2;
        int midY = DashY + DashH / 2;
        spriteBatch.Draw(_whitePixel, new Rectangle(midX - 1, DashY + 5, 2, DashH - 10), AmberDim);
        spriteBatch.Draw(_whitePixel, new Rectangle(CenterX + 5, midY - 1, CenterW - 10, 2), AmberDim);

        spriteBatch.Draw(_whitePixel, new Rectangle(midX - 3, DashY + 7, 1, 8), Amber);
        spriteBatch.Draw(_whitePixel, new Rectangle(midX + 2, DashY + 7, 1, 8), Amber);

        int seg = (int)((heading / twoPi) * 8 + 0.5f) % 8;
        int hlX = CenterX + seg * segW;
        spriteBatch.Draw(_whitePixel, new Rectangle(hlX + 5, DashY + 5, segW - 1, DashH - 10), new Color(30, 25, 10));

        var hlDsz = font.MeasureString(dirs[seg]);
        font.DrawString(spriteBatch, dirs[seg],
            new Vector2(hlX + segW / 2 - hlDsz.X / 2, DashY + DashH / 2 - hlDsz.Y / 2), Amber, 0.8f);

        font.DrawString(spriteBatch, "COMPASS",
            new Vector2(midX - 25, DashY + DashH - 25), AmberDim, 0.6f);
    }
}
