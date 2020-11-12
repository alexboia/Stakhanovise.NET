# Stakhanovise.NET Database Assets

Stakhanovise.NET relies on a couple of database assets to get the job done. 
You can find a script with all the required definitions [over here](https://github.com/alexboia/Stakhanovise.NET/blob/master/_Db/stakhanovise_db_scripts.sql) and their description right below.
Also, you don't actually have to set them up your self - the library will make sure everying is in place, 
but you can disable this behaviour if it doesn't suite your needs.

## Database assets schema

### 1. Tables

### The job queue table - sk_tasks_queue_t

| Column | Type | Notes |
| --- | --- | --- |
| `task_id` | `uuid` | `NOT NULL` |
| `task_type` | `character varying(250)` | `NOT NULL` |
| `task_source` | `character varying(250)` | `NOT NULL` |
| `task_payload` | `TEXT` | - |
| `task_status` | `integer` | `NOT NULL` |
| `task_priority` | `integer` | `NOT NULL` |
| `task_last_error` | `TEXT` | - |
| `task_error_count` | `integer` | `NOT NULL` |
| `task_last_error_is_recoverable` | `boolean` | `NOT NULL`, `DEFAULT false` |
| `task_processing_time_milliseconds` | `bigint` | `NOT NULL`, `DEFAULT 0` |
| `task_posted_at_ts` | `timestamp with time zone` | `NOT NULL` |
| `task_first_processing_attempted_at_ts` | `timestamp with time zone` | - |
| `task_last_processing_attempted_at_ts` | `timestamp with time zone` | - |
| `task_processing_finalized_at_ts` | `timestamp with time zone` | - |

### The job result table - sk_task_results_t

| Column | Type | Notes |
| --- | --- | --- |

### The application metrics table - sk_metrics_t 

| Column | Type | Notes |
| --- | --- | --- |

### The job execution performance data table - sk_task_execution_time_stats_t

| Column | Type | Notes |
| --- | --- | --- |

### 2. Functions

## Mapping


## Setting up the database