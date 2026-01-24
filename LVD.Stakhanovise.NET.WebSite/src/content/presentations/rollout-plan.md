---
title: Rollout Plan
tagline: Steps to deploy Stakhanovise.NET safely into production.
audience: SRE and platform teams
updated: "2026-01-24"
---

## Phased deployment

1. Stand up a staging queue with realistic load, enable tracing, and validate retry/backoff behavior.
2. Deploy a small worker pool to production in shadow mode; compare throughput and latency to your current pipeline.
3. Gradually migrate critical job types, watching dead-letter volume and queue lag.

## Observability checklist

- Expose queue depth, dequeue latency, and retry counts to your APM.
- Set alerts on dead-letter growth and lease reclamations.
- Wire health probes for worker heartbeat and storage availability.

## Operational guardrails

- Keep database credentials rotated and scoped to queue schemas.
- Ensure worker binaries start with exponential backoff and circuit-breakers for downstream dependencies.
- Review handler idempotency before enabling aggressive parallelism.
