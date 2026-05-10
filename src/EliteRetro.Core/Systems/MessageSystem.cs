namespace EliteRetro.Core.Systems;

/// <summary>
/// Implementation of IMessageSystem.
/// Manages timers and storage for on-screen text events.
/// </summary>
public class MessageSystem : IMessageSystem
{
    public string? GeneralMessage { get; private set; }
    public int GeneralTimer { get; private set; }

    public string? StatusMessage { get; private set; }
    public int StatusTimer { get; private set; }

    private bool _isMilestone;

    public void Post(string text, MessageType type, int duration = 120)
    {
        switch (type)
        {
            case MessageType.General:
                GeneralMessage = text;
                GeneralTimer = duration;
                _isMilestone = false;
                break;

            case MessageType.Milestone:
                GeneralMessage = text;
                GeneralTimer = duration;
                _isMilestone = true;
                break;

            case MessageType.Status:
                StatusMessage = text;
                StatusTimer = duration;
                break;
        }
    }

    public void Update()
    {
        if (GeneralTimer > 0)
        {
            GeneralTimer--;
            if (GeneralTimer == 0) GeneralMessage = null;
        }

        if (StatusTimer > 0)
        {
            StatusTimer--;
            if (StatusTimer == 0) StatusMessage = null;
        }
    }

    public void Clear()
    {
        GeneralMessage = null;
        GeneralTimer = 0;
        StatusMessage = null;
        StatusTimer = 0;
    }
}
