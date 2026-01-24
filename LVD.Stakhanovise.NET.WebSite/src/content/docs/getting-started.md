---
title: Getting Started
summary: Install Stakhanovise.NET, connect a database, and spin up your first worker brigade.
order: 1
---

## Install the packages

Add the core packages to your solution:

```bash
# SQL Server flavor
Install-Package Stakhanovise.NET.SqlServer

# PostgreSQL flavor
Install-Package Stakhanovise.NET.PostgreSql
```

## Configure the storage engine

Point the queue to your database. Stakhanovise.NET ships with schemas for SQL Server and PostgreSQL—apply them during your deployment pipeline.

```csharp
var storage = new SqlServerQueue("Server=tcp:queue.db,1433;Initial Catalog=Stakhanovise;...", new QueueOptions
{
    SchemaName = "stakhanovise",
    MaxDequeueBatchSize = 64
});
```

## Run a worker brigade

Workers are lightweight services that fetch jobs, execute handlers, and record outcomes.

```csharp
var worker = new QueueWorker(storage)
    .RegisterHandler<EmailJob>(async job => await emailSender.Send(job))
    .WithRetryPolicy(RetryPolicy.Exponential(maxAttempts: 6, baseDelay: TimeSpan.FromSeconds(5)));

await worker.StartAsync();
```

## Observe and tune

- Export metrics to Prometheus or Application Insights for throughput, retries, and dead-letter counts.
- Use health probes to track queue lag and worker heartbeat.
- Adjust batch size, visibility timeout, and retry backoff per queue to meet your SLAs.
