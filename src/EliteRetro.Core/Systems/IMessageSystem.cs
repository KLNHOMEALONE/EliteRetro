using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteRetro.Core.Systems;

public enum MessageType
{
    General,
    Milestone,
    Status
}

/// <summary>
/// Service for managing on-screen messages (events, milestones, status).
/// Handles timers and text storage.
/// </summary>
public interface IMessageSystem
{
    /// <summary>
    /// Add a message to the system.
    /// </summary>
    void Post(string text, MessageType type, int duration = 120);

    /// <summary>
    /// Update message timers.
    /// </summary>
    void Update();

    /// <summary>
    /// Clear all active messages.
    /// </summary>
    void Clear();

    /// <summary>
    /// Current General/Event message.
    /// </summary>
    string? GeneralMessage { get; }
    int GeneralTimer { get; }

    /// <summary>
    /// Current Status/Save message.
    /// </summary>
    string? StatusMessage { get; }
    int StatusTimer { get; }
}
