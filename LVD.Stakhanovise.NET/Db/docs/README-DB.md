# Stakhanovise.NET Database Assets

Stakhanovise.NET relies on a couple of database assets to get the job done. 
You can find a script with all the required definitions [over here](https://github.com/alexboia/Stakhanovise.NET/blob/master/_Db/stakhanovise_db_scripts.sql) and their description right below.
Also, you don't actually have to set them up your self - the library will make sure everying is in place, 
but you can disable this behaviour if it doesn't suite your needs.

## Sequence - `sk_tasks_queue_t_task_lock_handle_id_seq`

Used to feed values for the job queue table lock handle column values

| Name | Value |
| --- | --- |
| `start` | `1` |
| `increment` | `1` |
| `min_value` | `1` |
| `max_value` | `9223372036854775807` |
| `cache` | `1` |

## The application metrics table - `sk_metrics_t`

Stores the application metrics for the built-in app metrics writer.

| Column | Type | Notes |
| --- | --- | --- |
| `metric_id ` | `character varying(250)` | `NOT NULL` |
| `metric_owner_process_id ` | `character varying(250)` | `NOT NULL` |
| `metric_category ` | `character varying(150)` | `NOT NULL` |
| `metric_value ` | `bigint` | `NOT NULL`, `DEFAULT 0` |
| `metric_last_updated ` | `timestamp with time zone` | `NOT NULL`, `DEFAULT now()` |

## The job queue table - `sk_tasks_queue_t`

Stores the queued processing jobs

| Column | Type | Notes |
| --- | --- | --- |
| `task_id` | `uuid` | `NOT NULL`, `Primary Key` |
| `task_lock_handle_id` | `bigint` | `NOT NULL`, `Unique Key`, `DEFAULT nextval('public.sk_tasks_queue_t_task_lock_handle_id_seq'::regclass)` |
| `task_type` | `character varying(250)` | `NOT NULL` |
| `task_source` | `character varying(250)` | `NOT NULL` |
| `task_payload` | `text` | - |
| `task_priority` | `integer` | - |
| `task_posted_at_ts` | `timestamp with time zone` | `NOT NULL`, `DEFAULT now()` |
| `task_locked_until_ts` | `timestamp with time zone` | `NOT NULL` |

## The job execution performance data table - `sk_task_execution_time_stats_t`

Stores the job execution performance data for the built-in job execution performance data writer.

| Column | Type | Notes |
| --- | --- | --- |
| `et_payload_type` | `character varying(255)` | `NOT NULL`, `Primary Key` |
| `et_owner_process_id` | `character varying(255)` | `NOT NULL`, `Primary Key` |
| `et_n_execution_cycles` | `bigint` | `NOT NULL` |
| `et_last_execution_time` | `bigint` | `NOT NULL` |
| `et_avg_execution_time` | `bigint` | `NOT NULL` |
| `et_fastest_execution_time` | `bigint` | `NOT NULL` |
| `et_longest_execution_time` | `bigint` | `NOT NULL` |
| `et_total_execution_time` | `bigint` | `NOT NULL` |

## The job results table - `sk_task_results_t`

sStores processing job execution result meta-information, such as execution status.

| Column | Type | Notes |
| --- | --- | --- |
| `task_id` | `uuid` | `NOT NULL`, `Primary Key` |
| `task_type` | `character varying(250)` | `NOT NULL` |
| `task_source` | `character varying(250)` | `NOT NULL` |
| `task_payload` | `text` | - |
| `task_status` | `integer` | `NOT NULL` |
| `task_priority` | `integer` | `NOT NULL` |
| `task_last_error` | `text` | - |
| `task_error_count` | `integer` | `NOT NULL`, `DEFAULT 0` |
| `task_last_error_is_recoverable` | `boolean` | `NOT NULL`, `DEFAULT false` |
| `task_processing_time_milliseconds` | `bigint` | `NOT NULL`, `DEFAULT 0` |
| `task_posted_at_ts` | `timestamp with time zone` | `NOT NULL` |
| `task_first_processing_attempted_at_ts` | `timestamp with time zone` | - |
| `task_last_processing_attempted_at_ts` | `timestamp with time zone` | - |
| `task_processing_finalized_at_ts` | `timestamp with time zone` | - |

## The dequeue function - `sk_try_dequeue_task`

It is used to actually dequeue a job from the queue table

```
CREATE OR REPLACE FUNCTION public.sk_try_dequeue_task (IN select_types character varying[], IN exclude_ids uuid[], IN ref_now timestamp with time zone)
	RETURNS TABLE (task_id uuid, task_lock_handle_id bigint, task_type character varying, task_source character varying, task_payload text, task_priority integer, task_posted_at_ts timestamp with time zone, task_locked_until_ts timestamp with time zone)
	LANGUAGE plpgsql
	AS $$
	DECLARE
		n_select_types integer = CARDINALITY(select_types);
	BEGIN
		RETURN QUERY
		WITH sk_dequeued_task AS
			(DELETE FROM sk_tasks_queue_t td WHERE td.task_id = (
				SELECT t0.task_id
						FROM sk_tasks_queue_t t0
						WHERE (t0.task_type = ANY(select_types) OR n_select_types = 0)
							AND t0.task_locked_until_ts < ref_now
							AND t0.task_id <> ALL(exclude_ids)
						ORDER BY t0.task_priority ASC,
							t0.task_locked_until_ts ASC,
							t0.task_lock_handle_id ASC
						LIMIT 1
						FOR UPDATE SKIP LOCKED
			) RETURNING *) SELECT sdt.* FROM sk_dequeued_task sdt;
	END;
$$;
```

The function parameters are explained below:

| Parameter | Type | Notes |
| --- | --- | --- |
| `select_types` | `character varying[]` | An array of job types to use when dequeuing. Can be an empty array. |
| `exclude_ids` | `uuid[]` | An array of job IDs to exclude when dequeuing. Can be an empty array. |
| `ref_now` | `timestamp with time zone` | The timestamp to use as the current time. |

