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

