---
title: Architecture Blueprint
summary: Understand the moving parts that keep Stakhanovise.NET resilient under load.
order: 2
---

## Control plane vs. workers

Stakhanovise.NET separates orchestration from execution:

- **Storage engine**: Durable queue tables for jobs, leases, and dead letters.
- **Workers**: Pull jobs, lock them with visibility timeouts, execute handlers, and report outcomes.
- **Timers**: Reclaim abandoned leases, schedule retries, and flush dead letters.

## Safety rails

- Visibility timeouts prevent double-processing while allowing recovery when a worker disappears.
- Progress-aware retries apply increasing backoff and cap the maximum attempts.
- Poison jobs are routed to a dead-letter queue with the full failure record.

## Extensibility points

- Pluggable serializers for payloads and headers.
- Hooks for custom telemetry (structured logs, metrics, tracing spans).
- Policy knobs for queue isolation, batch sizing, and concurrency limits.

## Deployment model

- Run workers as Windows services, Linux systemd units, or containers.
- Point multiple worker pools at the same queue for horizontal scale.
- Keep database connections close to the workers to reduce dequeue latency.
