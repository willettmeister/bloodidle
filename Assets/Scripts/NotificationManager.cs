// Schedules and cancels push notifications.
// Requires: com.unity.mobile.notifications (install via Package Manager for device builds).
using UnityEngine;
#if UNITY_ANDROID && HAVE_MOBILE_NOTIFICATIONS
using Unity.Notifications.Android;
#endif
#if UNITY_IOS && HAVE_MOBILE_NOTIFICATIONS
using Unity.Notifications.iOS;
#endif

public static class NotificationManager
{
    const string ChannelId        = "bloodidle_idle";
    const string DailyChannelId   = "bloodidle_daily";

    public static void ScheduleIdleReminder(bool playerIsIdle)
    {
        CancelAll();
        ScheduleDailyQuestReminder();
        if (!playerIsIdle) return;

#if UNITY_ANDROID && HAVE_MOBILE_NOTIFICATIONS
        var channel = new AndroidNotificationChannel
        {
            Id          = ChannelId,
            Name        = "Idle Reminder",
            Importance  = Importance.Default,
            Description = "Reminds you to return to battle",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        var notification = new AndroidNotification
        {
            Title     = "BloodIdle",
            Text      = "Your soldiers are idle — return to battle!",
            FireTime  = System.DateTime.Now.AddHours(2),
            SmallIcon = "default",
            LargeIcon = "default",
        };
        AndroidNotificationCenter.SendNotification(notification, ChannelId);
#elif UNITY_IOS && HAVE_MOBILE_NOTIFICATIONS
        var request = new iOSNotificationTimeIntervalTrigger { TimeInterval = System.TimeSpan.FromHours(2) };
        var notification = new iOSNotification
        {
            Title    = "BloodIdle",
            Body     = "Your soldiers are idle — return to battle!",
            Trigger  = request,
            ShowInForeground = false,
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    static void ScheduleDailyQuestReminder()
    {
        // Fire at the next UTC midnight + 1 minute so quests have reset.
        var now   = System.DateTime.UtcNow;
        var reset = new System.DateTime(now.Year, now.Month, now.Day, 0, 1, 0, System.DateTimeKind.Utc).AddDays(1);
        var local = reset.ToLocalTime();

#if UNITY_ANDROID && HAVE_MOBILE_NOTIFICATIONS
        var channel = new AndroidNotificationChannel
        {
            Id          = DailyChannelId,
            Name        = "Daily Quests",
            Importance  = Importance.Default,
            Description = "Reminds you when daily quests and bonuses reset",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        var notification = new AndroidNotification
        {
            Title     = "BloodIdle — Daily Reset",
            Text      = "Daily quests and bonus are ready!",
            FireTime  = local,
            SmallIcon = "default",
            LargeIcon = "default",
        };
        AndroidNotificationCenter.SendNotification(notification, DailyChannelId);
#elif UNITY_IOS && HAVE_MOBILE_NOTIFICATIONS
        var delay   = local - System.DateTime.Now;
        if (delay.TotalSeconds > 0)
        {
            var trigger = new iOSNotificationTimeIntervalTrigger { TimeInterval = delay };
            var notification = new iOSNotification
            {
                Title    = "BloodIdle — Daily Reset",
                Body     = "Daily quests and bonus are ready!",
                Trigger  = trigger,
                ShowInForeground = false,
            };
            iOSNotificationCenter.ScheduleNotification(notification);
        }
#endif
    }

    public static void CancelAll()
    {
#if UNITY_ANDROID && HAVE_MOBILE_NOTIFICATIONS
        AndroidNotificationCenter.CancelAllScheduledNotifications();
#elif UNITY_IOS && HAVE_MOBILE_NOTIFICATIONS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
    }
}
