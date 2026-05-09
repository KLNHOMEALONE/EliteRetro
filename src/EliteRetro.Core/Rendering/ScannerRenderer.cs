using EliteRetro.Core.Entities;
using EliteRetro.Core.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// 3D elliptical scanner display — "space compass" from original Elite.
/// Shows nearby ships as dots with sticks indicating 3D position.
/// </summary>
public class ScannerRenderer
{
    public const int MaxRange = 63;

    private readonly Texture2D _whitePixel;
    private int _currentRadiusX;

    public ScannerRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager, int playerSlot, OrientationMatrix universeOrientation, Rectangle centerPanelRect)
    {
        // Derive scanner geometry from available center panel area.
        // Keep a wide ellipse like the original, but responsive to panel size.
        // Scale down by 1.2 for a more compact display
        float scaleDown = 1f / 1.2f;
        int centerX = centerPanelRect.X + centerPanelRect.Width / 2;
        int centerY = centerPanelRect.Y + centerPanelRect.Height / 2;
        int pad = Math.Clamp((int)MathF.Round(MathF.Min(centerPanelRect.Width, centerPanelRect.Height) * 0.06f), 6, 18);
        int maxRx = Math.Max(10, centerPanelRect.Width / 2 - pad);
        int maxRy = Math.Max(8, centerPanelRect.Height / 2 - pad);
        int radiusX = (int)(Math.Clamp((int)MathF.Round(centerPanelRect.Width * 0.47f), 10, maxRx) * scaleDown);
        int radiusY = (int)(Math.Clamp((int)MathF.Round(centerPanelRect.Height * 0.42f), 8, maxRy) * scaleDown);
        _currentRadiusX = radiusX;

        // Black background for scanner area
        var bg = new Rectangle(centerX - radiusX - 10, centerY - radiusY - 10, radiusX * 2 + 20, radiusY * 2 + 20);
        bg = Rectangle.Intersect(bg, centerPanelRect);
        if (bg.Width > 0 && bg.Height > 0)
            spriteBatch.Draw(_whitePixel, bg, Color.Black);

        // Dashed grey grid lines
        Color gridColor = new Color(80, 80, 80);
        // 3 horizontal lines
        int topWidth = (int)(radiusX * MathF.Sqrt(Math.Max(0, 1 - (radiusY * radiusY / 4f) / (radiusY * radiusY))));
        DrawDashedLine(spriteBatch, centerX - topWidth, centerY - radiusY / 2, centerX + topWidth, centerY - radiusY / 2, gridColor);
        DrawDashedLine(spriteBatch, centerX - radiusX, centerY, centerX + radiusX, centerY, gridColor);
        DrawDashedLine(spriteBatch, centerX - topWidth, centerY + radiusY / 2, centerX + topWidth, centerY + radiusY / 2, gridColor);
        // Vertical line from bottom to center
        DrawDashedLine(spriteBatch, centerX, centerY + radiusY, centerX, centerY, gridColor);

        // W shape inscribed in ellipse
        float wPeakY = centerY - radiusY * 0.85f;
        float wBottom = centerY + radiusY / 2;
        int bottomLineHalfWidth = topWidth;
        float peakYFrac = (wPeakY - centerY) / radiusY;
        float wXPeak = radiusX * MathF.Sqrt(Math.Max(0, 1 - peakYFrac * peakYFrac));
        float wXInner = wXPeak * 0.4f;
        DrawDashedLine(spriteBatch, centerX - bottomLineHalfWidth, (int)wBottom, (int)(centerX - wXInner), (int)wPeakY, gridColor);
        DrawDashedLine(spriteBatch, (int)(centerX - wXInner), (int)wPeakY, centerX, centerY, gridColor);
        DrawDashedLine(spriteBatch, centerX, centerY, (int)(centerX + wXInner), (int)wPeakY, gridColor);
        DrawDashedLine(spriteBatch, (int)(centerX + wXInner), (int)wPeakY, centerX + bottomLineHalfWidth, (int)wBottom, gridColor);

        // White ellipse outline (solid)
        DrawEllipse(spriteBatch, centerX, centerY, radiusX, radiusY, Color.White);

        // Sun indicator: small circle in upper left area
        DrawSunIndicator(spriteBatch, bubbleManager, centerX, centerY, radiusX, radiusY);

        // Station indicator: circle with square in upper right area
        DrawStationIndicator(spriteBatch, bubbleManager, centerX, centerY, radiusX, radiusY);

        // Ship contacts (transformed by universe orientation)
        DrawContacts(spriteBatch, bubbleManager, playerSlot, universeOrientation, centerX, centerY, radiusX, radiusY);
    }

    /// <summary>
    /// Draw sun indicator: small circle in upper left.
    /// Shows sun direction when sun is present (not replaced by station).
    /// </summary>
    private void DrawSunIndicator(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager, int centerX, int centerY, int radiusX, int radiusY)
    {
        var sun = bubbleManager.SunOrStation;
        if (sun == null || sun.Blueprint?.Name != "Sun")
            return;

        // Position: upper left area of scanner, proportional to ellipse size
        int indX = centerX - radiusX + (int)(radiusX * 0.25f);
        int indY = centerY - radiusY + (int)(radiusY * 0.35f);
        int radius = (int)(MathF.Min(radiusX, radiusY) * 0.18f);

        // Circle outline in orange/yellow
        DrawCircleOutline(spriteBatch, indX, indY, radius, Color.Orange);

        // Larger dot in center
        int dotSize = Math.Max(2, radius / 4);
        spriteBatch.Draw(_whitePixel, new Rectangle(indX - dotSize, indY - dotSize, dotSize * 2, dotSize * 2), Color.Yellow);
    }

    /// <summary>
    /// Draw station indicator: circle with square (Coriolis symbol).
    /// Filled square = station ahead (in safe zone), outlined = station behind.
    /// </summary>
    private void DrawStationIndicator(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager, int centerX, int centerY, int radiusX, int radiusY)
    {
        var station = bubbleManager.SunOrStation;
        if (station == null || station.Blueprint?.Name != "Coriolis Station")
            return;

        // Position: upper right area of scanner, inside ellipse, proportional to size
        int indX = centerX + radiusX - (int)(radiusX * 0.25f);
        int indY = centerY - radiusY + (int)(radiusY * 0.35f);
        int radius = (int)(MathF.Min(radiusX, radiusY) * 0.20f);
        int sqSize = (int)(radius * 1.2f);

        // Circle outline
        DrawCircleOutline(spriteBatch, indX, indY, radius, Color.Cyan);

        // Square: filled if station is ahead (in safe zone), outlined if behind
        bool inSafeZone = bubbleManager.IsInSafeZone();
        if (inSafeZone)
        {
            // Filled square
            spriteBatch.Draw(_whitePixel, new Rectangle(indX - sqSize / 2, indY - sqSize / 2, sqSize, sqSize), Color.Cyan);
        }
        else
        {
            // Outlined square (2px thick lines)
            spriteBatch.Draw(_whitePixel, new Rectangle(indX - sqSize / 2, indY - sqSize / 2, sqSize, 2), Color.Cyan); // top
            spriteBatch.Draw(_whitePixel, new Rectangle(indX - sqSize / 2, indY + sqSize / 2 - 2, sqSize, 2), Color.Cyan); // bottom
            spriteBatch.Draw(_whitePixel, new Rectangle(indX - sqSize / 2, indY - sqSize / 2, 2, sqSize), Color.Cyan); // left
            spriteBatch.Draw(_whitePixel, new Rectangle(indX + sqSize / 2 - 2, indY - sqSize / 2, 2, sqSize), Color.Cyan); // right
        }
    }

    /// <summary>
    /// Draw a circle outline.
    /// </summary>
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

    private void DrawEllipse(SpriteBatch spriteBatch, int cx, int cy, int rx, int ry, Color color)
    {
        const int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * MathF.Tau;
            float a2 = ((i + 1) / (float)segments) * MathF.Tau;

            int x1 = cx + (int)(MathF.Cos(a1) * rx);
            int y1 = cy + (int)(MathF.Sin(a1) * ry);
            int x2 = cx + (int)(MathF.Cos(a2) * rx);
            int y2 = cy + (int)(MathF.Sin(a2) * ry);

            DrawLine(spriteBatch, x1, y1, x2, y2, color);
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, int x1, int y1, int x2, int y2, Color color)
    {
        Vector2 dir = new Vector2(x2 - x1, y2 - y1);
        float len = dir.Length();
        if (len < 0.5f) return;

        dir /= len;
        float angle = MathF.Atan2(dir.Y, dir.X);
        spriteBatch.Draw(_whitePixel, new Rectangle(x1, y1, (int)len, 1), null, color, angle, Vector2.Zero, SpriteEffects.None, 0f);
    }

    private void DrawDashedLine(SpriteBatch spriteBatch, int x1, int y1, int x2, int y2, Color color)
    {
        Vector2 dir = new Vector2(x2 - x1, y2 - y1);
        float len = dir.Length();
        if (len < 1f) return;

        dir /= len;
        float angle = MathF.Atan2(dir.Y, dir.X);

        float refRadiusX = 180f;
        float scale = MathF.Max(0.5f, MathF.Min(2f, _currentRadiusX / refRadiusX));

        float dashLen = 6f * scale;
        float gapLen = 4f * scale;
        float pos = 0f;
        bool dash = true;

        while (pos < len)
        {
            float segLen = Math.Min(dash ? dashLen : gapLen, len - pos);
            if (dash && segLen > 0.5f)
            {
                int sx = x1 + (int)(dir.X * pos);
                int sy = y1 + (int)(dir.Y * pos);
                spriteBatch.Draw(_whitePixel, new Rectangle(sx, sy, (int)segLen, 1), null, color, angle, Vector2.Zero, SpriteEffects.None, 0f);
            }
            pos += segLen;
            dash = !dash;
        }
    }

    /// <summary>
    /// Project a ship's position (in player's local coordinate frame) to scanner screen coordinates.
    /// Expects: X = lateral (positive = right), Y = altitude (positive = up), Z = depth (positive = ahead).
    /// Returns the dot position and stick base for rendering.
    ///
    /// Follows BBC Elite's SCAN routine conventions (https://elite.bbcelite.com/deep_dives/the_3d_scanner.html):
    ///   X1 = center + x_hi          (lateral: right of player → right on scanner)
    ///   SC = center - z_hi / 4      (depth: ahead of player → top of scanner)
    ///   A  = -(y_hi / 2)            (altitude: above player → stick goes up from base)
    ///
    /// The original BBC Elite scanner was 138×36 pixels with x_hi in [-63, 63].
    /// This scanner is larger (RadiusX×2 by RadiusY×2), so all three axes are
    /// proportionally scaled to fill the ellipse while preserving the BBC Elite
    /// depth-to-lateral ratio (1:4) and altitude-to-lateral ratio (1:2).
    /// </summary>
    public static (Vector2 dotPos, Vector2 stickBase, bool visible)? ProjectToScanner(Vector3 localPos, int centerX, int centerY, int radiusX, int radiusY)
    {
        // Scale world units to scanner range [-MaxRange, MaxRange]
        float scale = MaxRange / 1000f;

        int xHi = (int)(localPos.X * scale);
        int yHi = (int)(localPos.Y * scale);
        int zHi = (int)(localPos.Z * scale);

        xHi = Math.Clamp(xHi, -MaxRange, MaxRange);
        yHi = Math.Clamp(yHi, -MaxRange, MaxRange);
        zHi = Math.Clamp(zHi, -MaxRange, MaxRange);

        // Lateral position: positive X (right of player) → right on scanner
        // Matches BBC Elite: X1 = 123 + x_hi, scaled to our scanner width
        int screenX = centerX + (int)(xHi * (radiusX / (float)MaxRange));

        // Depth position: positive Z (ahead of player) → top of scanner (smaller Y)
        // BBC Elite: SC = 220 - z_hi/4, where z_hi/4 spans ~88% of the 18-pixel half-height.
        // We scale depth to fill the same proportion of our larger scanner.
        float depthScale = radiusY * 0.875f / MaxRange;
        int stickBaseY = centerY - (int)(zHi * depthScale);

        // Altitude (stick height): positive Y (above player) → negative stick (dot above base)
        // BBC Elite: A = -(y_hi/2). Stick sensitivity is 2x depth sensitivity.
        float stickScale = depthScale * 2f;
        int stickHeight = -(int)(yHi * stickScale);

        // Ensure minimum stick length for visibility
        if (stickHeight == 0)
        {
            int minStick = Math.Max(4, (int)(depthScale * 4));
            stickHeight = (zHi > 0) ? -minStick : minStick;
        }

        int dotY = stickBaseY + stickHeight;

        // --- Visibility test ---
        // BBC Elite checks the stick BASE against the ellipse (the x/z plane position),
        // not the dot (which includes altitude). If the base is outside the ellipse,
        // the ship is too far away on the scanner plane and we skip it entirely.
        float baseDx = screenX - centerX;
        float baseDy = stickBaseY - centerY;
        float baseEllipseDist = (baseDx * baseDx) / (radiusX * radiusX) + (baseDy * baseDy) / (radiusY * radiusY);
        if (baseEllipseDist > 1.1f)
            return null;

        // --- Dot Y clamping ---
        // BBC Elite clamps the dot's screen Y to the dashboard boundaries (194-246)
        // so sticks of distant ships get shortened but still appear. We clamp to
        // our scanner's bounding box (with 1px margin for the dot size).
        int scannerTop = centerY - radiusY + 1;
        int scannerBot = centerY + radiusY - 1;
        dotY = Math.Clamp(dotY, scannerTop, scannerBot);

        return (new Vector2(screenX, dotY), new Vector2(screenX, stickBaseY), true);
    }

    /// <summary>
    /// Draw a ship contact — dot with vertical stick connecting to scanner plane.
    /// The stick runs from stickBase (on the Z-depth line) to the dot (Y-offset position).
    /// Horizontal tick at dot shows lateral movement direction.
    /// All sizes proportional to scanner dimensions.
    /// </summary>
    private void DrawContact(SpriteBatch spriteBatch, Vector2 dotPos, Vector2 stickBase, Color color)
    {
        // Scale contact elements proportionally to scanner size (based on 1024x768 reference)
        float refRadiusX = 180f; // reference scanner radiusX at 1024x768
        float scale = MathF.Max(0.5f, MathF.Min(2f, _currentRadiusX / refRadiusX)); // clamp scale factor

        int stickWidth = Math.Max(1, (int)(3 * scale));
        int tickWidth = Math.Max(4, (int)(8 * scale));
        int tickHeight = Math.Max(2, (int)(3 * scale));
        int dotSize = Math.Max(2, (int)(3 * scale));

        // Vertical stick from base (scanner plane) to dot
        int stickX = (int)stickBase.X;
        int stickTop = Math.Min((int)stickBase.Y, (int)dotPos.Y);
        int stickBot = Math.Max((int)stickBase.Y, (int)dotPos.Y);
        int stickW = Math.Max(1, stickBot - stickTop);
        if (stickW > 1)
            spriteBatch.Draw(_whitePixel, new Rectangle(stickX - stickWidth / 2, stickTop, stickWidth, stickW), color);

        // Horizontal tick at dot position
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - tickWidth / 2, (int)dotPos.Y - tickHeight / 2, tickWidth, tickHeight), color);

        // Dot at the end
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - dotSize / 2, (int)dotPos.Y - dotSize / 2, dotSize, dotSize), color);
    }

    private void DrawContacts(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager, int playerSlot, OrientationMatrix universeOrientation, int centerX, int centerY, int radiusX, int radiusY)
    {
        var player = bubbleManager.PlayerShip;
        if (player == null) return;

        foreach (var contact in bubbleManager.GetAllActive())
        {
            if (contact.SlotIndex == playerSlot || contact.SlotIndex < 2) continue;

            // Transform world-space delta into player's local coordinate frame
            // Transform returns: X=side(right), Y=roof(up), Z=nose(forward)
            Vector3 rel = contact.Position - player.Position;
            Vector3 basisCoords = universeOrientation.Transform(rel);
            // Scanner expects: Z positive = ahead.
            // In our local-bubble sim, ships ahead of player use negative Z (see FlightScene spawning/movement),
            // so flip Z here to match the scanner convention.
            Vector3 scannerLocal = new Vector3(basisCoords.X, basisCoords.Y, -basisCoords.Z);

            var pos = ProjectToScanner(scannerLocal, centerX, centerY, radiusX, radiusY);
            if (pos.HasValue)
            {
                // Station appears as a large dot on the scanner
                bool isStation = contact.Blueprint.Name == "Coriolis Station";
                if (isStation)
                {
                    DrawStationContact(spriteBatch, pos.Value.dotPos, pos.Value.stickBase);
                }
                else
                {
                    Color color;
                    if (contact.Blueprint.IsRock)
                        color = Color.Gray;
                    else if ((contact.Blueprint.ShipClass & (byte)NewbFlags.Hostile) == 0)
                        color = Color.Lime;
                    else
                        color = Color.OrangeRed;
                    DrawContact(spriteBatch, pos.Value.dotPos, pos.Value.stickBase, color);
                }
            }
        }
    }

    /// <summary>
    /// Draw station as a large dot on the scanner (bigger than ship contacts).
    /// All sizes proportional to scanner dimensions.
    /// </summary>
    private void DrawStationContact(SpriteBatch spriteBatch, Vector2 dotPos, Vector2 stickBase)
    {
        Color color = Color.White;

        // Scale proportional to scanner size (based on 1024x768 reference)
        float refRadiusX = 180f;
        float scale = MathF.Max(0.5f, MathF.Min(2f, _currentRadiusX / refRadiusX));

        int stickWidth = Math.Max(1, (int)(3 * scale));
        int tickWidth = Math.Max(4, (int)(8 * scale));
        int tickHeight = Math.Max(2, (int)(3 * scale));
        int dotSize = Math.Max(3, (int)(5 * scale));

        // Vertical stick from base to dot
        int stickX = (int)stickBase.X;
        int stickTop = Math.Min((int)stickBase.Y, (int)dotPos.Y);
        int stickBot = Math.Max((int)stickBase.Y, (int)dotPos.Y);
        int stickH = Math.Max(1, stickBot - stickTop);
        if (stickH > 1)
            spriteBatch.Draw(_whitePixel, new Rectangle(stickX - stickWidth / 2, stickTop, stickWidth, stickH), color);

        // Horizontal tick at dot position
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - tickWidth / 2, (int)dotPos.Y - tickHeight / 2, tickWidth, tickHeight), color);

        // Large dot for station
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - dotSize / 2, (int)dotPos.Y - dotSize / 2, dotSize, dotSize), color);
    }
}
