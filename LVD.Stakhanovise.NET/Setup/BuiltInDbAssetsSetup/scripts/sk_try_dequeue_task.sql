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
