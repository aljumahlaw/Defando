using Microsoft.AspNetCore.Components;

namespace Defando.Services;

/// <summary>
/// Service implementation for displaying toast notifications in Blazor UI.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly List<Notification> _notifications = new();
    
    /// <summary>
    /// Event that is triggered when a notification is added or removed.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Gets all current notifications.
    /// </summary>
    public IReadOnlyList<Notification> Notifications => _notifications.AsReadOnly();

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    public void ShowSuccess(string message, int duration = 3000)
    {
        AddNotification(new Notification
        {
            Message = message,
            Type = NotificationType.Success,
            Duration = duration
        });
    }

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    public void ShowError(string message, int duration = 5000)
    {
        AddNotification(new Notification
        {
            Message = message,
            Type = NotificationType.Error,
            Duration = duration
        });
    }

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    public void ShowWarning(string message, int duration = 4000)
    {
        AddNotification(new Notification
        {
            Message = message,
            Type = NotificationType.Warning,
            Duration = duration
        });
    }

    /// <summary>
    /// Shows an info notification.
    /// </summary>
    public void ShowInfo(string message, int duration = 3000)
    {
        AddNotification(new Notification
        {
            Message = message,
            Type = NotificationType.Info,
            Duration = duration
        });
    }

    /// <summary>
    /// Removes a notification by ID.
    /// </summary>
    public void RemoveNotification(string id)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            _notifications.Remove(notification);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Clears all notifications.
    /// </summary>
    public void ClearAll()
    {
        _notifications.Clear();
        NotifyStateChanged();
    }

    /// <summary>
    /// Adds a notification and schedules its removal.
    /// </summary>
    private void AddNotification(Notification notification)
    {
        _notifications.Add(notification);
        NotifyStateChanged();

        // Auto-remove notification after duration
        _ = Task.Run(async () =>
        {
            await Task.Delay(notification.Duration);
            RemoveNotification(notification.Id);
        });
    }

    /// <summary>
    /// Notifies subscribers of state changes.
    /// </summary>
    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}

