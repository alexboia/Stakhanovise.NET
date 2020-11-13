# Stakhanovise.NET Built-In App Metrics

Stakhanovise.NET exports the following application metrics. 
Their identifiers can be consulted [over here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Model/AppMetricId.cs).
Alll application metric values are expressed as 64bit integer values (`Int64`/`long`).

| Category | ID | AppMetricId | Notes |
| --- | --- | --- | --- |
| Notification listener (`task-queue-notification-listener`) | `listener@task-notification-count` | `AppMetricId.ListenerTaskNotificationCount` | How many new job notifications have been received by the notification listener. |
|  | `listener@reconnect-count` | `AppMetricId.ListenerReconnectCount` | How many times has the notification listener lost (and subsequently re-acquired) the listening connection. |
|  | `listener@notification-wait-timeout-count` | `AppMetricId.ListenerNotificationWaitTimeoutCount` | How many times cycles has the notification listener waited without receiving any notification (one timed-out wait cycle = one wait cycle without notifications). |
| --- | --- | --- | --- |
| Job poller (`task-poller`) | `poller@dequeue-count` | `AppMetricId.PollerDequeueCount` | How many jobs have been dequeued by the poller. |
|  | `poller@wait-dequeue-count` | `AppMetricId.PollerWaitForDequeueCount` | How many times has the poller waited for jobs to become available. |
|  | `poller@wait-buffer-space-count` | `AppMetricId.PollerWaitForBufferSpaceCount` | How many times has the poller waited for buffer space to become available. |
| --- | --- | --- | --- |
| Worker (`task-worer`) | `worker@processed-payload-count` | `AppMetricId.WorkerProcessedPayloadCount` | How many jobs have been processed by the workers. |
|  | `worker@buffer-wait-count` | `AppMetricId.WorkerBufferWaitCount` | How many times have the workers have waited for jobs to become available. |
|  | `worker@total-processing-time` | `AppMetricId.WorkerTotalProcessingTime` | How much time have the workers spent performing actual job processing. |
|  | `worker@successful-processed-payload-count` | `AppMetricId.WorkerSuccessfulProcessedPayloadCount` | How many jobs have been successfully processed. |
|  | `worker@failed-processed-payload-count` | `AppMetricId.WorkerFailedProcessedPayloadCount` | How many jobs have failed. |
|  | `worker@processing-cancelled-payload-count` | `AppMetricId.WorkerProcessingCancelledPayloadCount` | How many jobs have been cancelled. |
