CREATE SEQUENCE IF NOT EXISTS public.sk_tasks_queue_t_task_lock_handle_id_seq
	START WITH 1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	CACHE 1
	NO CYCLE;

CREATE TABLE IF NOT EXISTS public.sk_metrics_t(
	metric_id  character varying(250) NOT NULL,
	metric_owner_process_id  character varying(250) NOT NULL,
	metric_category  character varying(150) NOT NULL,
	metric_value  bigint DEFAULT 0 NOT NULL,
	metric_last_updated  timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY public.sk_metrics_t
	ADD CONSTRAINT pk_sk_metrics_t
	PRIMARY KEY (metric_id,metric_owner_process_id);

CREATE INDEX idx_sk_metrics_t_category
	ON public.sk_metrics_t USING btree
	(metric_category ASC);

CREATE TABLE IF NOT EXISTS public.sk_tasks_queue_t(
	task_id uuid NOT NULL,
	task_lock_handle_id bigint DEFAULT nextval('public.sk_tasks_queue_t_task_lock_handle_id_seq'::regclass) NOT NULL,
	task_type character varying(250) NOT NULL,
	task_source character varying(250) NOT NULL,
	task_payload text,
	task_priority integer,
	task_posted_at_ts timestamp with time zone DEFAULT now() NOT NULL,
	task_locked_until_ts timestamp with time zone NOT NULL
);

ALTER TABLE ONLY public.sk_tasks_queue_t
	ADD CONSTRAINT pk_sk_tasks_queue_t
	PRIMARY KEY (task_id);

ALTER TABLE ONLY public.sk_tasks_queue_t
	ADD CONSTRAINT unq_sk_tasks_queue_t_task_lock_handle_id
	UNIQUE (task_lock_handle_id);

CREATE INDEX idx_sk_tasks_queue_t_filter_index
	ON public.sk_tasks_queue_t USING btree
	(task_type ASC, task_locked_until_ts ASC);

CREATE INDEX idx_sk_tasks_queue_t_sort_index
	ON public.sk_tasks_queue_t USING btree
	(task_priority ASC, task_locked_until_ts ASC, task_lock_handle_id ASC);

CREATE TABLE IF NOT EXISTS public.sk_task_execution_time_stats_t(
	et_payload_type character varying(255) NOT NULL,
	et_owner_process_id character varying(255) NOT NULL,
	et_n_execution_cycles bigint NOT NULL,
	et_last_execution_time bigint NOT NULL,
	et_avg_execution_time bigint NOT NULL,
	et_fastest_execution_time bigint NOT NULL,
	et_longest_execution_time bigint NOT NULL,
	et_total_execution_time bigint NOT NULL
);

ALTER TABLE ONLY public.sk_task_execution_time_stats_t
	ADD CONSTRAINT pk_sk_task_execution_time_stats_t
	PRIMARY KEY (et_payload_type,et_owner_process_id);

CREATE TABLE IF NOT EXISTS public.sk_task_results_t(
	task_id uuid NOT NULL,
	task_type character varying(250) NOT NULL,
	task_source character varying(250) NOT NULL,
	task_payload text,
	task_status integer NOT NULL,
	task_priority integer NOT NULL,
	task_last_error text,
	task_error_count integer DEFAULT 0 NOT NULL,
	task_last_error_is_recoverable boolean DEFAULT false NOT NULL,
	task_processing_time_milliseconds bigint DEFAULT 0 NOT NULL,
	task_posted_at_ts timestamp with time zone NOT NULL,
	task_first_processing_attempted_at_ts timestamp with time zone,
	task_last_processing_attempted_at_ts timestamp with time zone,
	task_processing_finalized_at_ts timestamp with time zone
);

ALTER TABLE ONLY public.sk_task_results_t
	ADD CONSTRAINT pk_sk_task_results_t
	PRIMARY KEY (task_id);

CREATE INDEX idx_sk_task_results_t_task_status
	ON public.sk_task_results_t USING btree
	(task_status ASC);

CREATE INDEX idx_sk_task_results_t_task_type
	ON public.sk_task_results_t USING btree
	(task_type ASC);

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
