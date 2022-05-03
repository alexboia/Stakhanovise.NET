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

