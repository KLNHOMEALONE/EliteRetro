using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Rendering;

/// <summary>
/// Helpers for the virtual 1024x768 render target that all scenes draw to,
/// then blit onto the backbuffer with letterbox/pillarbox math preserving
/// the virtual aspect ratio.
/// </summary>
public static class VirtualFrame
{
    /// <summary>
    /// Compute the centered destination rectangle on the backbuffer that preserves
    /// the virtual frame's aspect ratio. If the backbuffer is wider than the
    /// virtual aspect, vertical pillarbox bars appear on the left/right. If the
    /// backbuffer is taller, horizontal letterbox bars appear top/bottom.
    /// </summary>
    public static Rectangle GetBackbufferDestRect(int screenW, int screenH, int vW, int vH)
    {
        float scale = Math.Min(screenW / (float)vW, screenH / (float)vH);
        int destW = (int)(vW * scale);
        int destH = (int)(vH * scale);
        int destX = (screenW - destW) / 2;
        int destY = (screenH - destH) / 2;
        return new Rectangle(destX, destY, destW, destH);
    }
}
