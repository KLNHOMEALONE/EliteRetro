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

    private static readonly Color Amber = new Color(255, 180, 50);
    private static readonly Color AmberDim = new Color(100, 70, 20);
    private static readonly Color PanelBg = new Color(8, 8, 12);
    private static readonly Color PanelBorder = new Color(40, 35, 25);
    private static readonly Color LabelBg = Color.Black;
    private static readonly Color LabelText = Color.White;
    private static readonly Color BarFill = Color.White;
    private static readonly Color BarBg = new Color(160, 20, 20); // deep red background

    // Visual tuning: spacing between label box and bar, plus extra padding inside panels.
    private const int LabelBarGapDefault = 10;
    private const int PanelInnerPadDefault = 6;

    public HudRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, HUDState state, BitmapFont font, Rectangle hudRect, Rectangle screenRect)
    {
        // Panel layout: left 1/4, center 1/2, right 1/4 (like original Elite)
        int leftW = (int)MathF.Round(hudRect.Width * 0.25f);
        int rightW = leftW;
        int centerW = hudRect.Width - leftW - rightW;
        int leftX = hudRect.X;
        int centerX = leftX + leftW;
        int rightX = centerX + centerW;

        int barSlotH = Math.Max(1, hudRect.Height / 7);

        spriteBatch.Draw(_whitePixel, hudRect, PanelBg);
        // Thin internal dividers (keeps the panel structured regardless of resolution)
        spriteBatch.Draw(_whitePixel, new Rectangle(hudRect.X, hudRect.Y, hudRect.Width, 2), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(centerX - 1, hudRect.Y, 2, hudRect.Height), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(rightX - 1, hudRect.Y, 2, hudRect.Height), PanelBorder);

        DrawLeftBars(spriteBatch, state, font, new Rectangle(leftX, hudRect.Y, leftW, hudRect.Height), barSlotH);
        DrawRightBars(spriteBatch, state, font, new Rectangle(rightX, hudRect.Y, rightW, hudRect.Height), barSlotH);

        // Note: top-of-screen overlay (view mode, status, legal, rank)
        // is drawn by the scene so it can be clipped to the view frame.
    }

    /// <summary>
    /// Draw indicators that sit ON TOP of the scanner area.
    /// </summary>
    public void DrawCenterOverlays(SpriteBatch spriteBatch, HUDState state, Rectangle hudRect)
    {
        int leftW = (int)MathF.Round(hudRect.Width * 0.25f);
        int rightW = leftW;
        int centerW = hudRect.Width - leftW - rightW;
        int centerX = hudRect.X + leftW;
        Rectangle centerRect = new Rectangle(centerX, hudRect.Y, centerW, hudRect.Height);

        DrawStatusBulbs(spriteBatch, state, centerRect);
        DrawSmallCompass(spriteBatch, state.TargetBearing, centerRect);
    }

    private void DrawLeftBars(SpriteBatch spriteBatch, HUDState state, BitmapFont font, Rectangle leftRect, int barSlotH)
    {
        int barY = leftRect.Y + 2;
        int barH = Math.Max(6, barSlotH - 4);
        
        // Match Legend: Labels are thin, but must fit text. 15% is a better balance.
        int labelW = Math.Clamp((int)MathF.Round(leftRect.Width * 0.15f), 24, 48);
        int gap = 4;
        int innerPad = 4;
        int barMaxW = Math.Max(10, leftRect.Width - labelW - (gap + innerPad) - 4);

        float fwdRatio = MathHelper.Clamp(state.ShieldForward / 255f, 0, 1);
        DrawBarH(spriteBatch, leftRect.X + 2, barY, labelW, barH, (int)(fwdRatio * barMaxW), barMaxW, "FS", font, gap);
        barY += barSlotH;

        float aftRatio = MathHelper.Clamp(state.ShieldAft / 255f, 0, 1);
        DrawBarH(spriteBatch, leftRect.X + 2, barY, labelW, barH, (int)(aftRatio * barMaxW), barMaxW, "RS", font, gap);
        barY += barSlotH;

        float fuelRatio = MathHelper.Clamp(state.Fuel / 70f, 0, 1);
        DrawBarH(spriteBatch, leftRect.X + 2, barY, labelW, barH, (int)(fuelRatio * barMaxW), barMaxW, "FU", font, gap);
        barY += barSlotH;

        float cabinRatio = MathHelper.Clamp(state.CabinTemp / 255f, 0, 1);
        DrawBarH(spriteBatch, leftRect.X + 2, barY, labelW, barH, (int)(cabinRatio * barMaxW), barMaxW, "CT", font, gap);
        barY += barSlotH;

        float laserRatio = MathHelper.Clamp(state.LaserTemp / 255f, 0, 1);
        DrawBarH(spriteBatch, leftRect.X + 2, barY, labelW, barH, (int)(laserRatio * barMaxW), barMaxW, "LT", font, gap);
        barY += barSlotH;

        float altRatio = MathHelper.Clamp(state.Altitude / 255f, 0, 1);
        DrawBarH(spriteBatch, leftRect.X + 2, barY, labelW, barH, (int)(altRatio * barMaxW), barMaxW, "AL", font, gap);
        barY += barSlotH;

        // Bottom slot: Missile Icon (Label) + Missile Indicators + Lock Box
        DrawMissileIcon(spriteBatch, leftRect.X + 2, barY, labelW, barH);
        DrawMissileBar(spriteBatch, leftRect.X + 2 + labelW + gap, barY, barMaxW, barH, state.Missiles, state.TargetLocked);
    }

    private void DrawMissileBar(SpriteBatch spriteBatch, int x, int y, int w, int h, int count, bool locked)
    {
        // Red background for the entire bar
        spriteBatch.Draw(_whitePixel, new Rectangle(x, y, w, h), BarBg);

        // Lock indicator: square on the far right
        int lockSize = h - 4;
        int lockX = x + w - lockSize - 2;
        int lockY = y + 2;
        Color lockColor = locked ? Color.White : Color.Black;
        spriteBatch.Draw(_whitePixel, new Rectangle(lockX, lockY, lockSize, lockSize), lockColor);

        // Vertical missile icons
        int availableW = w - lockSize - 6;
        int missileW = Math.Max(4, availableW / 4 - 4);
        int missileH = h - 6;
        int spacing = (availableW - (missileW * 4)) / 5;

        for (int i = 0; i < 4; i++)
        {
            if (i < count)
            {
                int mx = x + spacing + i * (missileW + spacing);
                int my = y + 3;
                
                // Draw vertical missile icon
                // Body
                spriteBatch.Draw(_whitePixel, new Rectangle(mx + 1, my + 3, missileW - 2, missileH - 3), Color.White);
                // Tip
                DrawLine(spriteBatch, mx + 1, my + 3, mx + missileW / 2, my, Color.White);
                DrawLine(spriteBatch, mx + missileW - 1, my + 3, mx + missileW / 2, my, Color.White);
                // Fins
                spriteBatch.Draw(_whitePixel, new Rectangle(mx, my + missileH - 2, missileW, 2), Color.White);
            }
        }

        // Border around bar
        spriteBatch.Draw(_whitePixel, new Rectangle(x - 1, y + 1, 1, h - 2), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(x + w, y + 1, 1, h - 2), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(x - 1, y + 1, w + 2, 1), PanelBorder);
        spriteBatch.Draw(_whitePixel, new Rectangle(x - 1, y + h - 2, w + 2, 1), PanelBorder);
    }

    private void DrawRightBars(SpriteBatch spriteBatch, HUDState state, BitmapFont font, Rectangle rightRect, int barSlotH)
    {
        int barY = rightRect.Y + 2;
        int barH = Math.Max(6, barSlotH - 4);
        int labelW = Math.Clamp((int)MathF.Round(rightRect.Width * 0.15f), 24, 48);
        int gap = 4;
        int innerPad = 4;
        int barMaxW = Math.Max(10, rightRect.Width - labelW - (gap + innerPad) - 4);

        float speedRatio = MathHelper.Clamp(state.Speed / GameConstants.SpeedMax, 0, 1);
        DrawBarH(spriteBatch, rightRect.X + 2, barY, labelW, barH, (int)(speedRatio * barMaxW), barMaxW, "SP", font, gap, textRight: true, labelOnRight: true);
        barY += barSlotH;

        float rollNorm = (state.Roll + 1) / 2;
        DrawBarV(spriteBatch, rightRect.X + 2, barY, labelW, barH, rollNorm, "RL", font, barMaxW, gap, textRight: true, labelOnRight: true);
        barY += barSlotH;

        float pitchNorm = (state.Pitch + 1) / 2;
        DrawBarV(spriteBatch, rightRect.X + 2, barY, labelW, barH, pitchNorm, "DC", font, barMaxW, gap, textRight: true, labelOnRight: true);
        barY += barSlotH;

        // Slots 1-4: Energy Banks (Additional Shield Units)
        // state.EnergyBanks is 0-16. Mapping: 16 banks -> 4 units full. 4 banks per bar.
        for (int i = 1; i <= 4; i++)
        {
            float unitBanks = MathHelper.Clamp(state.EnergyBanks - (i - 1) * 4, 0, 4);
            int unitFillW = (int)((unitBanks / 4f) * barMaxW);
            DrawBarH(spriteBatch, rightRect.X + 2, barY, labelW, barH, unitFillW, barMaxW, i.ToString(), font, gap, textCenter: true, labelOnRight: true);
            barY += barSlotH;
        }
    }

    private void DrawMissileIcon(SpriteBatch spriteBatch, int x, int y, int w, int h)
    {
        // Simple graphical missile icon in the label area
        int midY = y + h / 2;
        int iconW = (int)(w * 0.7f);
        int iconH = (int)(h * 0.5f);
        int iconX = x + (w - iconW) / 2;
        int iconY = midY - iconH / 2;

        // Missile body
        spriteBatch.Draw(_whitePixel, new Rectangle(iconX, iconY + 2, iconW - 4, iconH - 4), LabelText);
        // Tip
        DrawLine(spriteBatch, iconX + iconW - 4, iconY + 2, iconX + iconW, midY, LabelText);
        DrawLine(spriteBatch, iconX + iconW - 4, iconY + iconH - 2, iconX + iconW, midY, LabelText);
        // Fins
        spriteBatch.Draw(_whitePixel, new Rectangle(iconX, iconY, 2, iconH), LabelText);
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
        int gap,
        bool textRight = false,
        bool textCenter = false,
        bool labelOnRight = false)
    {
        // Layout: either [label][gap][bar] (default) or [bar][gap][label] (labelOnRight)
        int labelX = labelOnRight ? x + maxW + gap : x;
        int barX = labelOnRight ? x : x + labelW + gap;

        // Label background (black) + text (white)
        spriteBatch.Draw(_whitePixel, new Rectangle(labelX, y, labelW, h), LabelBg);
        if (!string.IsNullOrEmpty(label))
        {
            var lsz = font.MeasureString(label);
            float textX =
                textCenter ? labelX + (labelW - lsz.X) / 2f :
                textRight ? labelX + labelW - lsz.X - 2 :
                labelX + 2;
            DrawThickText(spriteBatch, font, label,
                new Vector2(textX, y + (h - lsz.Y) / 2),
                LabelText, 1.2f);
        }

        // Bar area: red background full height, white fill with visible padding
        int barPad = Math.Clamp(h / 6, 1, 6);
        int barTopPad = barPad;
        int barBotPad = barPad;
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
        int barW,
        int gap,
        bool textRight = false,
        bool labelOnRight = false)
    {
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

    private void DrawStatusBulbs(SpriteBatch spriteBatch, HUDState state, Rectangle centerRect)
    {
        // Bulbs are positioned in the top-left area of the center panel
        // Use a unified radius for both bulb and compass for balance
        int unifiedRadius = Math.Max(8, (int)(centerRect.Height * 0.12f));
        
        // Match Legend: Push further toward the top-left corner
        int bulbX = centerRect.X + (int)(centerRect.Width * 0.08f);
        int bulbY = centerRect.Y + (int)(centerRect.Height * 0.15f);

        // Status bulb (Green/Red/Yellow based on danger/station)
        Color statusColor = state.StationInView ? Color.Yellow : Color.Lime;
        DrawCircleFilled(spriteBatch, bulbX, bulbY, unifiedRadius, statusColor);

        // Fuel scoop / Docking bulb (Yellow icon below status)
        if (state.HasFuelScoop || state.HasDockingComputer)
        {
            int iconY = bulbY + unifiedRadius + 10;
            // Draw a simple "canister" shape for scoop
            spriteBatch.Draw(_whitePixel, new Rectangle(bulbX - (int)(unifiedRadius * 0.6f), iconY, (int)(unifiedRadius * 1.2f), (int)(unifiedRadius * 1.2f)), Color.Yellow);
            // "Top" of the canister
            spriteBatch.Draw(_whitePixel, new Rectangle(bulbX - (int)(unifiedRadius * 0.8f), iconY - 2, (int)(unifiedRadius * 1.6f), 3), Color.Yellow);
        }
    }

    private void DrawSmallCompass(SpriteBatch spriteBatch, Vector2 bearing, Rectangle centerRect)
    {
        // Small compass is a circle with a dot in the top-right area
        int unifiedRadius = Math.Max(8, (int)(centerRect.Height * 0.12f));
        
        // Match Legend: Push further toward the top-right corner
        int compX = centerRect.Right - (int)(centerRect.Width * 0.08f);
        int compY = centerRect.Y + (int)(centerRect.Height * 0.15f);

        // Compass outline
        DrawCircleOutline(spriteBatch, compX, compY, unifiedRadius, Color.White);
        
        // Target dot: bearing is -1 to 1 on both axes
        float dist = bearing.Length();
        Vector2 clampedBearing = bearing;
        if (dist > 1.0f) clampedBearing /= dist;
        
        int dotX = compX + (int)(clampedBearing.X * (unifiedRadius - 4));
        int dotY = compY + (int)(clampedBearing.Y * (unifiedRadius - 4));
        int dotSize = Math.Max(2, unifiedRadius / 4);
        
        spriteBatch.Draw(_whitePixel, new Rectangle(dotX - dotSize / 2, dotY - dotSize / 2, dotSize, dotSize), Color.White);
    }

    private void DrawCircleFilled(SpriteBatch spriteBatch, int cx, int cy, int radius, Color color)
    {
        // Simple filled circle via overlapping rectangles
        for (int y = -radius; y <= radius; y++)
        {
            int x = (int)MathF.Sqrt(radius * radius - y * y);
            spriteBatch.Draw(_whitePixel, new Rectangle(cx - x, cy + y, x * 2, 1), color);
        }
    }

    private void DrawCircleOutline(SpriteBatch spriteBatch, int cx, int cy, int radius, Color color)
    {
        const int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * MathF.Tau;
            float a2 = ((i + 1) / (float)segments) * MathF.Tau;
            int x1 = cx + (int)(MathF.Cos(a1) * radius);
            int y1 = cy + (int)(MathF.Sin(a1) * radius);
            int x2 = cx + (int)(MathF.Cos(a2) * radius);
            int y2 = cy + (int)(MathF.Sin(a2) * radius);
            DrawLine(spriteBatch, x1, y1, x2, y2, color);
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, int x1, int y1, int x2, int y2, Color color)
    {
        Vector2 edge = new Vector2(x2 - x1, y2 - y1);
        float angle = MathF.Atan2(edge.Y, edge.X);
        spriteBatch.Draw(_whitePixel, new Vector2(x1, y1), null, color, angle, Vector2.Zero, new Vector2(edge.Length(), 1), SpriteEffects.None, 0);
    }

    private static void DrawThickText(SpriteBatch spriteBatch, BitmapFont font, string text, Vector2 pos, Color color, float scale)
    {
        // Bitmap font has no bold; emulate thickness with 1px overdraw.
        font.DrawString(spriteBatch, text, pos, color, scale);
        font.DrawString(spriteBatch, text, pos + new Vector2(1, 0), color, scale);
        font.DrawString(spriteBatch, text, pos + new Vector2(0, 1), color, scale);
    }
}
