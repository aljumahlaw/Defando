using System;
using System.Collections.Generic;

namespace Defando.Services
{
    /// <summary>
    /// Service interface for displaying toast notifications in the UI.
    /// </summary>
    public interface INotificationService
    {
        // قائمة الإشعارات الحالية (تستخدمها NotificationToast.razor)
        IReadOnlyList<Notification> Notifications { get; }

        // يُستدعى عندما تتغير قائمة الإشعارات
        event Action? OnChange;

        /// <summary>
        /// Shows a success notification.
        /// </summary>
        void ShowSuccess(string message, int duration = 3000);

        /// <summary>
        /// Shows an error notification.
        /// </summary>
        void ShowError(string message, int duration = 5000);

        /// <summary>
        /// Shows a warning notification.
        /// </summary>
        void ShowWarning(string message, int duration = 4000);

        /// <summary>
        /// Shows an info notification.
        /// </summary>
        void ShowInfo(string message, int duration = 3000);

        /// <summary>
        /// Removes a notification by ID.
        /// </summary>
        void RemoveNotification(string id);
    }

    /// <summary>
    /// Notification data model.
    /// </summary>
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public int Duration { get; set; } = 3000;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Notification types.
    /// </summary>
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }
}

