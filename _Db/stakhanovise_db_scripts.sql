--
-- PostgreSQL database dump
--

-- Dumped from database version 12.3
-- Dumped by pg_dump version 12.3

-- Started on 2020-10-30 17:25:42

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 2 (class 3079 OID 16607)
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- TOC entry 2864 (class 0 OID 0)
-- Dependencies: 2
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


--
-- TOC entry 218 (class 1255 OID 24901)
-- Name: sk_try_dequeue_task(character varying[], uuid[], timestamp with time zone); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.sk_try_dequeue_task(select_types character varying[], exclude_ids uuid[], ref_now timestamp with time zone) RETURNS TABLE(task_id uuid, task_lock_handle_id bigint, task_type character varying, task_source character varying, task_payload text, task_priority integer, task_posted_at_ts timestamp with time zone, task_locked_until_ts timestamp with time zone)
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


ALTER FUNCTION public.sk_try_dequeue_task(select_types character varying[], exclude_ids uuid[], ref_now timestamp with time zone) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 207 (class 1259 OID 24902)
-- Name: sk_metrics_t; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sk_metrics_t (
    metric_id character varying(250) NOT NULL,
    metric_category character varying(150) NOT NULL,
    metric_value bigint DEFAULT 0 NOT NULL,
    metric_last_updated timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.sk_metrics_t OWNER TO postgres;

--
-- TOC entry 203 (class 1259 OID 16588)
-- Name: sk_processing_queues_task_lock_handle_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.sk_processing_queues_task_lock_handle_id_seq
    START WITH 2704602
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.sk_processing_queues_task_lock_handle_id_seq OWNER TO postgres;

--
-- TOC entry 205 (class 1259 OID 16638)
-- Name: sk_task_execution_time_stats_t; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sk_task_execution_time_stats_t (
    et_payload_type character varying(255) NOT NULL,
    et_n_execution_cycles bigint NOT NULL,
    et_last_execution_time bigint NOT NULL,
    et_avg_execution_time bigint NOT NULL,
    et_fastest_execution_time bigint NOT NULL,
    et_longest_execution_time bigint NOT NULL,
    et_total_execution_time bigint NOT NULL
);


ALTER TABLE public.sk_task_execution_time_stats_t OWNER TO postgres;

--
-- TOC entry 206 (class 1259 OID 16651)
-- Name: sk_task_results_t; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sk_task_results_t (
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


ALTER TABLE public.sk_task_results_t OWNER TO postgres;

--
-- TOC entry 204 (class 1259 OID 16628)
-- Name: sk_tasks_queue_t; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sk_tasks_queue_t (
    task_id uuid NOT NULL,
    task_lock_handle_id bigint DEFAULT nextval('public.sk_processing_queues_task_lock_handle_id_seq'::regclass) NOT NULL,
    task_type character varying(250) NOT NULL,
    task_source character varying(250) NOT NULL,
    task_payload text,
    task_priority integer NOT NULL,
    task_posted_at_ts timestamp with time zone DEFAULT now() NOT NULL,
    task_locked_until_ts timestamp with time zone NOT NULL
);


ALTER TABLE public.sk_tasks_queue_t OWNER TO postgres;

--
-- TOC entry 2729 (class 2606 OID 16658)
-- Name: sk_task_results_t pk_sk_task_results_t_task_id; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sk_task_results_t
    ADD CONSTRAINT pk_sk_task_results_t_task_id PRIMARY KEY (task_id);


--
-- TOC entry 2721 (class 2606 OID 16636)
-- Name: sk_tasks_queue_t pk_sk_tasks_queue_t_task_id; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sk_tasks_queue_t
    ADD CONSTRAINT pk_sk_tasks_queue_t_task_id PRIMARY KEY (task_id);


--
-- TOC entry 2732 (class 2606 OID 24908)
-- Name: sk_metrics_t sk_metrics_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sk_metrics_t
    ADD CONSTRAINT sk_metrics_pkey PRIMARY KEY (metric_id);


--
-- TOC entry 2725 (class 2606 OID 16642)
-- Name: sk_task_execution_time_stats_t sk_task_execution_time_stats_t_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sk_task_execution_time_stats_t
    ADD CONSTRAINT sk_task_execution_time_stats_t_pkey PRIMARY KEY (et_payload_type);


--
-- TOC entry 2723 (class 2606 OID 24894)
-- Name: sk_tasks_queue_t unq_sk_tasks_queue_t_task_lock_handle_id; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sk_tasks_queue_t
    ADD CONSTRAINT unq_sk_tasks_queue_t_task_lock_handle_id UNIQUE (task_lock_handle_id);


--
-- TOC entry 2730 (class 1259 OID 24909)
-- Name: idx_sk_metrics_t_metric_category; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sk_metrics_t_metric_category ON public.sk_metrics_t USING btree (metric_category);


--
-- TOC entry 2726 (class 1259 OID 16660)
-- Name: idx_sk_task_results_t_task_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sk_task_results_t_task_status ON public.sk_task_results_t USING btree (task_status);


--
-- TOC entry 2727 (class 1259 OID 16659)
-- Name: idx_sk_task_results_t_task_type; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sk_task_results_t_task_type ON public.sk_task_results_t USING btree (task_type);


-- Completed on 2020-10-30 17:25:43

--
-- PostgreSQL database dump complete
--

