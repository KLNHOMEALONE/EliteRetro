using Microsoft.Xna.Framework;
using EliteRetro.Core.HUD;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Service for calculating the visual state of the HUD dashboard.
/// </summary>
public interface IHudService
{
    /// <summary>
    /// Calculate the full dashboard state based on current player and world data.
    /// </summary>
    HUDState CalculateState(
        IGameContext game,
        float playerSpeed,
        float pitchRate,
        float rollRate,
        float cumulativeRoll,
        int viewMode,
        string eventMessage,
        int eventMessageTimer,
        bool showHiddenEdges);
}
