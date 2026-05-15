# NotificationManager

Static class. Schedules and cancels platform push notifications. Called by `GameManager` on app pause/resume. Requires the `com.unity.mobile.notifications` package.

Assembly: `GameAssembly`  
File: `Assets/Scripts/NotificationManager.cs`

---

## Constants

| Constant | Value |
|----------|-------|
| `ChannelId` | `"bloodidle_idle"` |

---

## Methods

### `ScheduleIdleReminder(bool playerIsIdle)`

Schedules a "soldiers are idle" push notification 2 hours from now. Does nothing if `playerIsIdle` is `false`.

**Android**: Registers an `AndroidNotificationChannel` (`Importance.Default`), cancels any existing scheduled notifications, then sends a new one with `FireTime = Now + 2h`.

**iOS**: Cancels existing notifications, sends an `iOSNotification` with a `TimeIntervalTrigger` of 2 hours and `ShowInForeground = false`.

Called from `GameManager.OnApplicationPause(true)` — fires when the app backgrounds.

---

### `CancelAll()`

Cancels all scheduled notifications.

**Android**: `AndroidNotificationCenter.CancelAllScheduledNotifications()`  
**iOS**: `iOSNotificationCenter.RemoveAllScheduledNotifications()`

Called from `GameManager.OnApplicationPause(false)` — fires when the app resumes so the stale reminder is cleared.

---

## Platform guards

All notification code is inside `#if UNITY_ANDROID` / `#elif UNITY_IOS` blocks. The class compiles and runs on all platforms but does nothing on non-mobile targets (including the Unity Editor).
