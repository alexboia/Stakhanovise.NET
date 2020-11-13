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
|  | `worker@total-processing-time` | `AppMetricId.WorkerTotalProcessingTime` | How much time (measured in milliseconds) have the workers spent performing actual job processing. |
|  | `worker@successful-processed-payload-count` | `AppMetricId.WorkerSuccessfulProcessedPayloadCount` | How many jobs have been successfully processed. |
|  | `worker@failed-processed-payload-count` | `AppMetricId.WorkerFailedProcessedPayloadCount` | How many jobs have failed. |
|  | `worker@processing-cancelled-payload-count` | `AppMetricId.WorkerProcessingCancelledPayloadCount` | How many jobs have been cancelled. |
| --- | --- | --- | --- |
| Queue Consumer (`task-queue-consumer`) | `queue-consumer@dequeue-count` | `AppMetricId.QueueConsumerDequeueCount` | How many jobs has the queue consumer dequeued. |
|  | `queue-consumer@total-dequeue-duration` | `AppMetricId.QueueConsumerTotalDequeueDuration` | How much time (measured in milliseconds) has the queue consumer spent fetching jobs from the queue. |
|  | `queue-consumer@minimum-dequeue-duration` | `AppMetricId.QueueConsumerMinimumDequeueDuration` | The minimum time (measured in milliseconds) spent by the queue consumer to fetch jobs from the queue (i.e. the quickest dequeue operation). |
|  | `queue-consumer@maximum-dequeue-duration` | `AppMetricId.QueueConsumerMaximumDequeueDuration` | The maximum time (measured in milliseconds) spent by the queue consumer to fetch jobs from the queue (i.e. the slowest dequeue operation). |
| --- | --- | --- | --- |
| Result Queue (`task-result-queue`) | `result-queue@result-post-count` | `AppMetricId.ResultQueueResultPostCount` | How many job results have been posted. |
|  | `result-queue@result-writes-count` | `AppMetricId.ResultQueueResultWriteCount` | How many posted job results have actually been written. |
|  | `result-queue@maximum-result-write-duration` | `AppMetricId.ResultQueueMaximumResultWriteDuration` | The maximum time (measured in milliseconds) spent by the result queue to write a set of results (i.e. the slowest result write operation). |
|  | `result-queue@minimum-result-write-duration` | `AppMetricId.ResultQueueMinimumResultWriteDuration` | The minimum time (measured in milliseconds) spent by the result queue to write a set of results (i.e. the quickest result write operation). |
|  | `result-queue@total-result-write-duration` | `AppMetricId.ResultQueueTotalResultWriteDuration` | The time time (measured in milliseconds) spent by the result queue to write result sets. |
|  | `result-queue@result-write-rq-timeout-count` | `AppMetricId.ResultQueueResultWriteRequestTimeoutCount` | How many result write operations have timed out. |
| --- | --- | --- | --- |
| Buffer (`task-buffer`) | `task-buffer@max-count` | `AppMetricId.BufferMaxCount` | The maximum number of job payloads that have ever been held in buffer. |
|  | `task-buffer@min-count` | `AppMetricId.BufferMinCount` | The minimum number of job payloads that have ever been held in buffer. |
|  | `task-buffer@times-filled` | `AppMetricId.BufferTimesFilled` | How many times has the buffer been filled. |
|  | `task-buffer@times-emptied` | `AppMetricId.BufferTimesEmptied` | How many times has the buffer been emptied. |
| --- | --- | --- | --- |
| Job Execution Performance Monitoring (`execution-perf-mon`) | `perf-mon@report-post-count` | `AppMetricId.PerfMonReportPostCount` | How many execution performance reports have been posted. |
|  | `perf-mon@report-write-count` | `AppMetricId.PerfMonReportWriteCount` | How many posted execution performance reports have actually been written. |
|  | `perf-mon@minimum-report-write-duration` | `AppMetricId.PerfMonMinimumReportWriteDuration` | The minimum time (measured in milliseconds) spent writing a set of execution performance reports (i.e. quickest write operation). |
|  | `perf-mon@maximum-report-write-duration` | `AppMetricId.PerfMonMaximumReportWriteDuration` | The maximum time (measured in milliseconds) spent writing a set of execution performance reports (i.e. slowest write operation). |
|  | `perf-mon@report-requests-timeout-count` | `AppMetricId.PerfMonReportWriteRequestsTimeoutCount` | How many execution performance report write operations have timed out. |