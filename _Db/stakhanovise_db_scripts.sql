CREATE SEQUENCE public.sk_processing_queues_task_lock_handle_id_seq
    INCREMENT 1
    START 2704602
    MINVALUE 1
    MAXVALUE 9223372036854775807
    CACHE 1;

ALTER SEQUENCE public.sk_processing_queues_task_lock_handle_id_seq
    OWNER TO postgres;
	
-- Table: public.sk_tasks_queue_t
-- DROP TABLE public.sk_tasks_queue_t;

CREATE TABLE public.sk_tasks_queue_t
(
    task_id uuid NOT NULL,
    task_lock_handle_id bigint NOT NULL DEFAULT nextval('sk_processing_queues_task_lock_handle_id_seq'::regclass),
    task_type character varying(250) COLLATE pg_catalog."default" NOT NULL,
    task_source character varying(250) COLLATE pg_catalog."default" NOT NULL,
    task_payload text COLLATE pg_catalog."default",
    task_status integer NOT NULL,
    task_priority integer NOT NULL,
    task_last_error_is_recoverable boolean NOT NULL,
    task_error_count integer NOT NULL,
	task_locked_until bigint NOT NULL,
    task_processing_time_milliseconds bigint NOT NULL,
    task_posted_at bigint NOT NULL,
    task_posted_at_ts timestamp with time zone NOT NULL,
    task_reposted_at_ts timestamp with time zone NOT NULL,
    task_first_processing_attempted_at_ts timestamp with time zone,
    task_last_processing_attempted_at_ts timestamp with time zone,
    task_processing_finalized_at_ts timestamp with time zone,
    task_last_error text COLLATE pg_catalog."default",
    CONSTRAINT tasks_queue_t_pkey PRIMARY KEY (task_id)
)

TABLESPACE pg_default;

ALTER TABLE public.sk_tasks_queue_t
    OWNER to postgres;
	
-- Table: public.sk_time_t
-- DROP TABLE public.sk_time_t;

CREATE TABLE public.sk_time_t
(
    t_id uuid NOT NULL,
    t_total_ticks bigint,
    t_total_ticks_cost bigint,
    CONSTRAINT sk_time_t_pkey PRIMARY KEY (t_id)
)

TABLESPACE pg_default;

ALTER TABLE public.sk_time_t
    OWNER to postgres;
	
-- FUNCTION: public.sk_has_advisory_lock(bigint)
-- DROP FUNCTION public.sk_has_advisory_lock(bigint);

CREATE OR REPLACE FUNCTION public.sk_has_advisory_lock(
	lock_handle_id bigint)
    RETURNS boolean
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE 
AS $BODY$
declare 
	is_lock_held bool;
begin
	select (case when count(1) > 0 then true else false end) into is_lock_held
		from pg_locks l 
		where l.database = (select d.oid from pg_database d where d.datname = current_database() )
			and l.locktype = 'advisory'
			and l.objid = lock_handle_id
			and l.granted = true;
			
	return is_lock_held;
end;
$BODY$;

ALTER FUNCTION public.sk_has_advisory_lock(bigint)
    OWNER TO postgres;
	
-- FUNCTION: public.sk_try_dequeue_task(integer[], character varying[], uuid[], bigint)
-- DROP FUNCTION public.sk_try_dequeue_task(integer[], character varying[], uuid[], bigint);

CREATE OR REPLACE FUNCTION public.sk_try_dequeue_task(
	select_statuses integer[],
	select_types character varying[],
	exclude_ids uuid[],
	now bigint)
    RETURNS TABLE(task_id uuid, 
				  task_lock_handle_id bigint, 
				  task_type character varying, 
				  task_source character varying, 
				  task_payload text, 
				  task_status integer, 
				  task_priority integer, 
				  task_last_error_is_recoverable boolean, 
				  task_error_count integer, 
				  task_locked_until bigint,
				  task_processing_time_milliseconds bigint,
    			  task_posted_at bigint,
				  task_posted_at_ts timestamp with time zone, 
				  task_reposted_at_ts timestamp with time zone, 
				  task_first_processing_attempted_at_ts timestamp with time zone, 
				  task_last_processing_attempted_at_ts timestamp with time zone, 
				  task_processing_finalized_at_ts timestamp with time zone,
				  task_last_error text, 
				  is_lock_acquired boolean) 
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE 
    ROWS 1000
    
AS $BODY$

declare
	n_select_types integer = cardinality(select_types);
	n_select_statuses integer = cardinality(select_statuses);
	
begin
	return query 
	with recursive sk_tasks_queue_v as (
			(select t0.*, l0.is_lock_acquired
				from (select tq.* 
						from sk_tasks_queue_t tq 
						where (tq.task_status = any(select_statuses) or n_select_statuses = 0) 
					  		and (tq.task_type = any(select_types) or n_select_types = 0)
							and tq.task_id <> all(exclude_ids)
					  		and tq.task_locked_until < now
						order by tq.task_priority desc,
							tq.task_posted_at asc,
					  		tq.task_lock_handle_id asc
						limit 1) t0
				left join lateral (select pg_try_advisory_lock(t0.task_lock_handle_id) as is_lock_acquired) l0 
					on true) 

		union 

			(select t0r.*, l0r.is_lock_acquired
				from (select tq.* 
						from sk_tasks_queue_t tq 
						inner join sk_tasks_queue_v tqv 
							on tq.task_id <> tqv.task_id
							and tq.task_posted_at >= tqv.task_posted_at
							and tqv.is_lock_acquired = false 
						where (tq.task_status = any(select_statuses) or n_select_statuses = 0) 
					  		and (tq.task_type = any(select_types) or n_select_types = 0)
							and tq.task_id <> all(exclude_ids)
					  		and tq.task_locked_until < now
						order by tq.task_priority desc,
							tq.task_posted_at asc,
					  		tq.task_lock_handle_id asc
						limit 1) t0r
				left join lateral (select pg_try_advisory_lock(t0r.task_lock_handle_id) as is_lock_acquired) l0r
					on true)
	) select tqv.* 
		from sk_tasks_queue_v tqv
		where tqv.is_lock_acquired = true;
end;

$BODY$;

ALTER FUNCTION public.sk_try_dequeue_task(integer[], character varying[], uuid[], bigint)
    OWNER TO postgres;
	
-- FUNCTION: public.sk_compute_absolute_time_ticks(uuid, bigint)
-- DROP FUNCTION public.sk_compute_absolute_time_ticks(uuid, bigint);

CREATE OR REPLACE FUNCTION public.sk_compute_absolute_time_ticks(
	time_id uuid,
	time_ticks_to_add bigint)
    RETURNS bigint
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE 
    
AS $BODY$declare
	r_total_ticks bigint;
begin
	select st.t_total_ticks + time_ticks_to_add into r_total_ticks
	from sk_time_t st
	where st.t_id = time_id;
	
	return r_total_ticks;
end;$BODY$;

ALTER FUNCTION public.sk_compute_absolute_time_ticks(uuid, bigint)
    OWNER TO postgres;

