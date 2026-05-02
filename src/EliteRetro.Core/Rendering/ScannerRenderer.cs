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
    // Scanner geometry — fits full center dashboard panel between left/right bars
    // Dashboard center: X=256 to 768 (512 wide), Y=480 to 768 (288 tall)
    public const int CenterX = 512;   // 256 + 256
    public const int CenterY = 624;   // 480 + 144
    public const int RadiusX = 240;   // nearly full 512 width with margins
    public const int RadiusY = 120;   // nearly full 288 height with margins

    public const int MaxRange = 63;

    private readonly Texture2D _whitePixel;

    public ScannerRenderer(GraphicsDevice graphicsDevice)
    {
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager, int playerSlot, OrientationMatrix universeOrientation)
    {
        // Black background for scanner area
        spriteBatch.Draw(_whitePixel, new Rectangle(CenterX - RadiusX - 10, CenterY - RadiusY - 10, RadiusX * 2 + 20, RadiusY * 2 + 20), Color.Black);

        // Dashed grey grid lines
        Color gridColor = new Color(80, 80, 80);
        // 3 horizontal lines
        int topWidth = (int)(RadiusX * MathF.Sqrt(Math.Max(0, 1 - (RadiusY * RadiusY / 4f) / (RadiusY * RadiusY))));
        DrawDashedLine(spriteBatch, CenterX - topWidth, CenterY - RadiusY / 2, CenterX + topWidth, CenterY - RadiusY / 2, gridColor);
        DrawDashedLine(spriteBatch, CenterX - RadiusX, CenterY, CenterX + RadiusX, CenterY, gridColor);
        DrawDashedLine(spriteBatch, CenterX - topWidth, CenterY + RadiusY / 2, CenterX + topWidth, CenterY + RadiusY / 2, gridColor);
        // Vertical line from bottom to center
        DrawDashedLine(spriteBatch, CenterX, CenterY + RadiusY, CenterX, CenterY, gridColor);

        // W shape inscribed in ellipse
        float wPeakY = CenterY - RadiusY * 0.85f;
        float wBottom = CenterY + RadiusY / 2;
        int bottomLineHalfWidth = topWidth;
        float peakYFrac = (wPeakY - CenterY) / RadiusY;
        float wXPeak = RadiusX * MathF.Sqrt(Math.Max(0, 1 - peakYFrac * peakYFrac));
        float wXInner = wXPeak * 0.4f;
        DrawDashedLine(spriteBatch, CenterX - bottomLineHalfWidth, (int)wBottom, (int)(CenterX - wXInner), (int)wPeakY, gridColor);
        DrawDashedLine(spriteBatch, (int)(CenterX - wXInner), (int)wPeakY, CenterX, CenterY, gridColor);
        DrawDashedLine(spriteBatch, CenterX, CenterY, (int)(CenterX + wXInner), (int)wPeakY, gridColor);
        DrawDashedLine(spriteBatch, (int)(CenterX + wXInner), (int)wPeakY, CenterX + bottomLineHalfWidth, (int)wBottom, gridColor);

        // White ellipse outline (solid)
        DrawEllipse(spriteBatch, CenterX, CenterY, RadiusX, RadiusY, Color.White);

        // Sun indicator: small circle in upper left area
        DrawSunIndicator(spriteBatch, bubbleManager);

        // Station indicator: circle with square in upper right area
        DrawStationIndicator(spriteBatch, bubbleManager);

        // Ship contacts (transformed by universe orientation)
        DrawContacts(spriteBatch, bubbleManager, playerSlot, universeOrientation);
    }

    /// <summary>
    /// Draw sun indicator: small circle in upper left.
    /// Shows sun direction when sun is present (not replaced by station).
    /// </summary>
    private void DrawSunIndicator(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager)
    {
        var sun = bubbleManager.SunOrStation;
        if (sun == null || sun.Blueprint?.Name != "Sun")
            return;

        // Position: upper left area of scanner
        int indX = CenterX - RadiusX + 35;
        int indY = CenterY - RadiusY + 30;
        int radius = 14;

        // Circle outline in orange/yellow
        DrawCircleOutline(spriteBatch, indX, indY, radius, Color.Orange);

        // Larger dot in center
        spriteBatch.Draw(_whitePixel, new Rectangle(indX - 2, indY - 2, 4, 4), Color.Yellow);
    }

    /// <summary>
    /// Draw station indicator: circle with square (Coriolis symbol).
    /// Filled square = station ahead (in safe zone), outlined = station behind.
    /// </summary>
    private void DrawStationIndicator(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager)
    {
        var station = bubbleManager.SunOrStation;
        if (station == null || station.Blueprint?.Name != "Coriolis Station")
            return;

        // Position: upper right area of scanner, inside ellipse
        int indX = CenterX + RadiusX - 35;
        int indY = CenterY - RadiusY + 30;
        int radius = 16;
        int sqSize = 12;

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

        float dashLen = 6f;
        float gapLen = 4f;
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
    public static (Vector2 dotPos, Vector2 stickBase, bool visible)? ProjectToScanner(Vector3 localPos)
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
        int screenX = CenterX + (int)(xHi * (RadiusX / (float)MaxRange));

        // Depth position: positive Z (ahead of player) → top of scanner (smaller Y)
        // BBC Elite: SC = 220 - z_hi/4, where z_hi/4 spans ~88% of the 18-pixel half-height.
        // We scale depth to fill the same proportion of our larger scanner.
        float depthScale = RadiusY * 0.875f / MaxRange;
        int stickBaseY = CenterY - (int)(zHi * depthScale);

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
        float baseDx = screenX - CenterX;
        float baseDy = stickBaseY - CenterY;
        float baseEllipseDist = (baseDx * baseDx) / (RadiusX * RadiusX) + (baseDy * baseDy) / (RadiusY * RadiusY);
        if (baseEllipseDist > 1.1f)
            return null;

        // --- Dot Y clamping ---
        // BBC Elite clamps the dot's screen Y to the dashboard boundaries (194-246)
        // so sticks of distant ships get shortened but still appear. We clamp to
        // our scanner's bounding box (with 1px margin for the dot size).
        int scannerTop = CenterY - RadiusY + 1;
        int scannerBot = CenterY + RadiusY - 1;
        dotY = Math.Clamp(dotY, scannerTop, scannerBot);

        return (new Vector2(screenX, dotY), new Vector2(screenX, stickBaseY), true);
    }

    /// <summary>
    /// Draw a ship contact — dot with vertical stick connecting to scanner plane.
    /// The stick runs from stickBase (on the Z-depth line) to the dot (Y-offset position).
    /// Horizontal tick at dot shows lateral movement direction.
    /// </summary>
    private void DrawContact(SpriteBatch spriteBatch, Vector2 dotPos, Vector2 stickBase, Color color)
    {
        // Vertical stick from base (scanner plane) to dot (3px wide)
        int stickX = (int)stickBase.X;
        int stickTop = Math.Min((int)stickBase.Y, (int)dotPos.Y);
        int stickBot = Math.Max((int)stickBase.Y, (int)dotPos.Y);
        int stickW = Math.Max(1, stickBot - stickTop);
        if (stickW > 1)
            spriteBatch.Draw(_whitePixel, new Rectangle(stickX - 1, stickTop, 3, stickW), color);

        // Horizontal tick at dot position (8px wide, 2px tall)
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - 4, (int)dotPos.Y - 1, 8, 3), color);

        // 3-pixel dot at the end
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - 1, (int)dotPos.Y - 1, 3, 3), color);
    }

    private void DrawContacts(SpriteBatch spriteBatch, LocalBubbleManager bubbleManager, int playerSlot, OrientationMatrix universeOrientation)
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
            // Scanner needs: X=lateral(right), Y=altitude(up), Z=depth(ahead)
            // Transform already gives (right, up, forward) so use directly
            Vector3 scannerLocal = basisCoords;

            var pos = ProjectToScanner(scannerLocal);
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
    /// </summary>
    private void DrawStationContact(SpriteBatch spriteBatch, Vector2 dotPos, Vector2 stickBase)
    {
        Color color = Color.White;

        // Vertical stick from base to dot
        int stickX = (int)stickBase.X;
        int stickTop = Math.Min((int)stickBase.Y, (int)dotPos.Y);
        int stickBot = Math.Max((int)stickBase.Y, (int)dotPos.Y);
        int stickH = Math.Max(1, stickBot - stickTop);
        if (stickH > 1)
            spriteBatch.Draw(_whitePixel, new Rectangle(stickX - 1, stickTop, 3, stickH), color);

        // Horizontal tick at dot position (8px wide, 2px tall)
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - 4, (int)dotPos.Y - 1, 8, 3), color);

        // Large dot (5×5) for station
        spriteBatch.Draw(_whitePixel, new Rectangle((int)dotPos.X - 2, (int)dotPos.Y - 2, 5, 5), color);
    }
}
