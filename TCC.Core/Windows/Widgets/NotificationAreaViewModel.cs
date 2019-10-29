﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Threading;
using FoglioUtils;
using TCC.Settings;
using TCC.Utils;
using TCC.ViewModels;

namespace TCC.Windows.Widgets
{
    public class NotificationAreaViewModel : TccWindowViewModel
    {
        private static int _id;
        private readonly ConcurrentQueue<NotificationInfoBase> _queue;
        public TSObservableCollection<NotificationInfoBase> Notifications { get; }

        public NotificationAreaViewModel(WindowSettings settings) : base(settings)
        {
            _queue = new ConcurrentQueue<NotificationInfoBase>();
            Notifications = new TSObservableCollection<NotificationInfoBase>(Dispatcher);
            Log.NewNotification += Enqueue;
        }

        private void CheckShow()
        {
            Dispatcher.Invoke(() =>
            {
                while (Notifications.Count < ((NotificationAreaSettings)Settings).MaxNotifications)
                {
                    if (_queue.IsEmpty) break;
                    if (!_queue.TryDequeue(out var next)) continue;
                    if (!Pass(next)) continue;
                    Notifications.Add(next);
                }
            });
        }

        private bool Pass(NotificationInfoBase infoBase)
        {
            return Notifications.ToSyncList().All(n => n.Message != infoBase.Message);
        }

        private int Enqueue(string title, string message, NotificationType type, uint duration = 4000U, NotificationTemplate template = NotificationTemplate.Default)
        {
            switch (template)
            {
                case NotificationTemplate.Progress:
                    _queue.Enqueue(new ProgressNotificationInfo(_id, title, message, type, duration, template));
                    break;
                default:
                    _queue.Enqueue(new NotificationInfoBase(_id, title, message, type, duration, template));
                    break;
            }
            CheckShow();
            return _id++;
        }

        public void DeleteNotification(NotificationInfoBase dc)
        {
            Dispatcher.InvokeAsync(() =>
            {
                Notifications.Remove(dc);
                CheckShow();
            }, DispatcherPriority.Background);
        }

        public T GetNotification<T>(int notifId) where T : NotificationInfoBase
        {
            var ret = Notifications.ToSyncList().FirstOrDefault(x => x.Id == notifId);
            if (ret != null) return (T)ret;
            ret = _queue.ToArray().FirstOrDefault(x => x.Id == notifId);
            return (T)ret;
        }
    }
}