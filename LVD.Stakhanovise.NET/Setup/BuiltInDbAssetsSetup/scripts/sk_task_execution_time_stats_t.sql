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

ALTER TABLE sk_task_execution_time_stats_t DROP CONSTRAINT  IF EXISTS pk_sk_task_execution_time_stats_t CASCADE;
ALTER TABLE ONLY public.sk_task_execution_time_stats_t
	ADD CONSTRAINT pk_sk_task_execution_time_stats_t
	PRIMARY KEY (et_payload_type,et_owner_process_id);

