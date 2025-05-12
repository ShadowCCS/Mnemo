using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MnemoProject.Services
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        AIGeneration
    }

    public class Notification
    {
        public string Title { get; }
        public string Message { get; }
        public NotificationType Type { get; }
        public DateTime Timestamp { get; }
        public bool IsVisible { get; set; }
        public int Id { get; }

        public Notification(string message, NotificationType type, string title = null)
        {
            Title = title;
            Message = message;
            Type = type;
            Timestamp = DateTime.Now;
            IsVisible = true;
            Id = NotificationService.GetNextId();
        }
    }

    public static class NotificationService
    {
        private static readonly string LogFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        
        private static int _nextId = 1;
        private static readonly ObservableCollection<Notification> _notifications = new();
        
        /// <summary>
        /// Maximum number of notifications that can be displayed at once
        /// </summary>
        public static int MaxNotifications { get; set; } = 3;

        public static ObservableCollection<Notification> Notifications => _notifications;
        
        // Command for closing notifications from the UI
        public static RelayCommand<int> CloseNotification { get; } = new RelayCommand<int>(RemoveNotification);

        static NotificationService()
        {
            // Ensure log directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
            
            // Log application startup
            LogToFile($"Application started at {DateTime.Now}");
        }

        public static int GetNextId()
        {
            return _nextId++;
        }

        public static void ShowNotification(string message, NotificationType type = NotificationType.Info, string title = null)
        {
            var notification = new Notification(message, type, title);
            
            // Log to file
            LogToFile($"[{type}] {(title != null ? $"{title}: " : "")}  {message}");
            
            // Show on UI
            Dispatcher.UIThread.Post(() =>
            {
                // Check if we've reached the maximum number of notifications
                while (_notifications.Count >= MaxNotifications && _notifications.Count > 0)
                {
                    // Remove the oldest notification
                    _notifications.RemoveAt(0);
                }
                
                // Add the new notification
                _notifications.Add(notification);
                
                // Auto-remove notifications after a delay
                Task.Delay(type == NotificationType.Error ? 10000 : 5000).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_notifications.Contains(notification))
                        {
                            _notifications.Remove(notification);
                        }
                    });
                });
            });
        }

        public static void Info(string message, string title = null)
        {
            ShowNotification(message, NotificationType.Info, title);
        }

        public static void Success(string message, string title = null)
        {
            ShowNotification(message, NotificationType.Success, title);
        }

        public static void Warning(string message, string title = null)
        {
            ShowNotification(message, NotificationType.Warning, title);
        }

        public static void Error(string message, string title = null)
        {
            ShowNotification(message, NotificationType.Error, title);
        }
        
        public static void AIGeneration(string message, string title = null)
        {
            ShowNotification(message, NotificationType.AIGeneration, title);
        }

        public static void LogToFile(string message)
        {
            try
            {
                // Format: [2023-01-01 12:34:56] [INFO] Message
                string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                
                // Append to log file
                File.AppendAllText(LogFilePath, formattedMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If we can't log to file, there's not much we can do except write to debug output
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
        
        private static void RemoveNotification(int id)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var notification = _notifications.FirstOrDefault(n => n.Id == id);
                if (notification != null)
                {
                    _notifications.Remove(notification);
                }
            });
        }
    }
} 