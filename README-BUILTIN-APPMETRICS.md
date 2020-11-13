# Stakhanovise.NET Built-In App Metrics

Stakhanovise.NET exports the following application metrics. 
Their identifiers can be consulted [over here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Model/AppMetricId.cs).
Alll application metric values are expressed as 64bit integer values (`Int64`/`long`).

| Category | ID | AppMetricId | Notes |
| --- | --- | --- | --- |
| Notification listener (`task-queue-notification-listener`) | `listener@task-notification-count` | `AppMetricId.ListenerTaskNotificationCount` | How many new job notifications have been received by the notification listener. |
| Notification listener (`task-queue-notification-listener`) | `listener@reconnect-count` | `AppMetricId.ListenerReconnectCount` | How many times has the notification listener lost (and subsequently re-acquired) the listening connection. |
| Notification listener (`task-queue-notification-listener`) | `listener@notification-wait-timeout-count` | `AppMetricId.ListenerNotificationWaitTimeoutCount` | How many times cycles has the notification listener waited without receiving any notification (one timed-out wait cycle = one wait cycle without notifications). |
