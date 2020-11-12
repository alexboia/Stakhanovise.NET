# Stakhanovise.NET Database Assets

Stakhanovise.NET relies on a couple of database assets to get the job done. 
You can find a script with all the required definitions [over here](https://github.com/alexboia/Stakhanovise.NET/blob/master/_Db/stakhanovise_db_scripts.sql) and their description right below.
Also, you don't actually have to set them up your self - the library will make sure everying is in place, 
but you can disable this behaviour if it doesn't suite your needs.

## Database assets schema

### 1. Tables

### The job queue table - `sk_tasks_queue_t`

Stores the queued processing jobs.

| Column | Type | Notes |
| --- | --- | --- |
| `task_id` | `uuid` | `NOT NULL`, PK |
| `task_lock_handle_id` | `bigint` | `NOT NULL`, `DEFAULT nextval('public.sk_processing_queues_task_lock_handle_id_seq'::regclass)`, Unique  |
| `task_type` | `character varying(250)` | `NOT NULL` |
| `task_source` | `character varying(250)` | `NOT NULL` |
| `task_payload ` | `TEXT` | - |
| `task_priority` | `integer` | `NOT NULL` |
| `task_posted_at_ts` | `timestamp with time zone` | `NOT NULL`, `DEFAULT now()` |
| `task_locked_until_ts` | `timestamp with time zone` | `NOT NULL` |

### The job result table - `sk_task_results_t`

Stores processing job execution result meta-information, such as execution status.

| Column | Type | Notes |
| --- | --- | --- |
| `task_id` | `uuid` | `NOT NULL`, PK |
| `task_type` | `character varying(250)` | `NOT NULL`, Indexed (Non-Unique) |
| `task_source` | `character varying(250)` | `NOT NULL` |
| `task_payload` | `TEXT` | - |
| `task_status` | `integer` | `NOT NULL`, Indexed (Non-Unique) |
| `task_priority` | `integer` | `NOT NULL` |
| `task_last_error` | `TEXT` | - |
| `task_error_count` | `integer` | `NOT NULL` |
| `task_last_error_is_recoverable` | `boolean` | `NOT NULL`, `DEFAULT false` |
| `task_processing_time_milliseconds` | `bigint` | `NOT NULL`, `DEFAULT 0` |
| `task_posted_at_ts` | `timestamp with time zone` | `NOT NULL` |
| `task_first_processing_attempted_at_ts` | `timestamp with time zone` | - |
| `task_last_processing_attempted_at_ts` | `timestamp with time zone` | - |
| `task_processing_finalized_at_ts` | `timestamp with time zone` | - |

### The application metrics table - `sk_metrics_t`

Stores the application metrics for the built-in app metrics writer.

| Column | Type | Notes |
| --- | --- | --- |
| `metric_id` | `character varying(250)` | `NOT NULL`, PK |
| `metric_category` | `character varying(150)` | `NOT NULL`, Indexed (Non-Unique) |
| `metric_value` | `bigint` | `NOT NULL`, `DEFAULT 0` |
| `metric_last_updated` | `timestamp with time zone` | `NOT NULL`, `DEFAULT now()` |

### The job execution performance data table - `sk_task_execution_time_stats_t`

Stores the job execution performance data for the built-in job execution performance data writer.

| Column | Type | Notes |
| --- | --- | --- |
| `et_payload_type` | `character varying(255)` | `NOT NULL`, PK |
| `et_n_execution_cycles` | `bigint` | `NOT NULL` |
| `et_last_execution_time` | `bigint` | `NOT NULL` |
| `et_avg_execution_time` | `bigint` | `NOT NULL` |
| `et_fastest_execution_time` | `bigint` | `NOT NULL` |
| `et_longest_execution_time` | `bigint` | `NOT NULL` |
| `et_total_execution_time` | `bigint` | `NOT NULL` |

### 2. Functions

### The dequeue function - `sk_try_dequeue_task`

It is used to actually dequeue a job from the queue table. Defined as:

```
CREATE FUNCTION public.sk_try_dequeue_task(select_types character varying[], 
	exclude_ids uuid[], 
	ref_now timestamp with time zone) 
		RETURNS TABLE(task_id uuid, 
			task_lock_handle_id bigint, 
			task_type character varying, 
			task_source character varying, 
			task_payload text, 
			task_priority integer, 
			task_posted_at_ts timestamp with time zone, 
			task_locked_until_ts timestamp with time zone)
    LANGUAGE plpgsql
    AS $$

declare
	n_select_types integer = cardinality(select_types);
	
begin
	return query 
	with sk_dequeued_task as
		(delete from sk_tasks_queue_t td where td.task_id = (
			select t0.task_id
					from sk_tasks_queue_t t0 
					where (t0.task_type = any(select_types) or n_select_types = 0)
						and t0.task_id <> all(exclude_ids)
						and t0.task_locked_until_ts < ref_now
					order by t0.task_priority asc,
						t0.task_locked_until_ts asc,
						t0.task_lock_handle_id asc
					limit 1
					for update skip locked
		) returning *) select sdt.* from sk_dequeued_task sdt;
end;
$$;
```

The function's parameters are explained below:

| Parameter | Type | Notes |
| --- | --- | --- |
| `select_types` | `character varying[]` | An array of job types to use when dequeuing. Can be an empty array. |
| `exclude_ids` | `uuid[]` | An array of job IDs to exclude when dequeuing. Can be an empty array. |
| `ref_now` | `timestamp with time zone` | The timestamp to use as the current time. |
