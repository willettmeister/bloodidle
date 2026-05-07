// Schedules and cancels the "soldiers idle" push notification.
// Requires: com.unity.mobile.notifications (already in manifest).
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public static class NotificationManager
{
    const string ChannelId = "bloodidle_idle";

    public static void ScheduleIdleReminder(bool playerIsIdle)
    {
        if (!playerIsIdle) return;

#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel
        {
            Id          = ChannelId,
            Name        = "Idle Reminder",
            Importance  = Importance.Default,
            Description = "Reminds you to return to battle",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        CancelAll();

        var notification = new AndroidNotification
        {
            Title     = "BloodIdle",
            Text      = "Your soldiers are idle — return to battle!",
            FireTime  = System.DateTime.Now.AddHours(2),
            SmallIcon = "default",
            LargeIcon = "default",
        };
        AndroidNotificationCenter.SendNotification(notification, ChannelId);
#elif UNITY_IOS
        CancelAll();
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

    public static void CancelAll()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
    }
}
