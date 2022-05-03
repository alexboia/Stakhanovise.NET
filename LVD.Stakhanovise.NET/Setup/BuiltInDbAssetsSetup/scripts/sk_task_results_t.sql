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

