--
-- PostgreSQL database dump
--

-- Dumped from database version 9.6.11
-- Dumped by pg_dump version 9.6.11

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: plpgsql; Type: EXTENSION; Schema: -; Owner: 
--

CREATE EXTENSION IF NOT EXISTS plpgsql WITH SCHEMA pg_catalog;


--
-- Name: EXTENSION plpgsql; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION plpgsql IS 'PL/pgSQL procedural language';


SET default_tablespace = '';

SET default_with_oids = false;

--
-- Name: client_connections; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.client_connections (
    client_id integer,
    status smallint,
    connection_timestamp timestamp without time zone,
    ssh_ip text,
    ssh_port integer,
    ssh_forwarding integer,
    ssh_user text
);


ALTER TABLE public.client_connections OWNER TO postgres;

--
-- Name: clients; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.clients (
    id integer NOT NULL,
    is_device boolean,
    is_developer boolean,
    client_key text,
    client_name text
);


ALTER TABLE public.clients OWNER TO postgres;

--
-- Name: clients_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.clients_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.clients_id_seq OWNER TO postgres;

--
-- Name: clients_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.clients_id_seq OWNED BY public.clients.id;


--
-- Name: developer_authorizations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.developer_authorizations (
    developer_id integer,
    device_id integer
);


ALTER TABLE public.developer_authorizations OWNER TO postgres;

--
-- Name: device_requests; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.device_requests (
    client_id integer,
    is_requested boolean,
    request_timestamp timestamp without time zone,
    requested_by_client_id integer
);


ALTER TABLE public.device_requests OWNER TO postgres;

--
-- Name: clients id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clients ALTER COLUMN id SET DEFAULT nextval('public.clients_id_seq'::regclass);


--
-- Data for Name: client_connections; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.client_connections (client_id, status, connection_timestamp, ssh_ip, ssh_port, ssh_forwarding, ssh_user) FROM stdin;
3	0	2020-09-02 12:31:00	127.0.0.1	22	50000	xxx
1	0	2020-09-02 12:26:05	127.0.0.1	22	50001	xxx
2	0	2020-10-07 17:33:16	192.168.0.106	22	50000	pi
\.


--
-- Data for Name: clients; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.clients (id, is_device, is_developer, client_key, client_name) FROM stdin;
1	t	f	TnLmmiGb7q/aeaIPWxw5hOVKLoYqxch4l6kbSSHv8QA=	94e56d06-eb96-44ba-a0ce-c8c384086c71
4	f	t	ta1V7sIB2U+TbLD1vHJQbBDAj+72sTlnyeSS5a31XmE=	873bbd58-0932-4e48-987e-99e1c7aeb6af
2	t	f	U8a2xaaYz2sNhEGDO9T4Ms9Wf4AWMQv+gDpmYJx+YmI=	50148590-1b48-4cf5-a76d-8a7f9474a3de
3	t	f	HeeFfpsSelN8c5zIJuZn7mQ28MBGIAoqp8Nf94N2eGM=	d457db4f-b931-4e91-96bb-adbd86f4ba6d
5	f	t	anI4ICTj9bs+gNQRa3aBbbQmsYCGvNIKB1qTkWZoj/k=	378ce77c-5b45-4126-9dfa-0371daa51563
6	f	t	4izf3TH3lTWS3gEhGhiIkJDEUX9+0VJfbmeE06xNRCU=	6f5decbc-87b7-441f-8015-9c178226f4e3
\.


--
-- Name: clients_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.clients_id_seq', 5, true);


--
-- Data for Name: developer_authorizations; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.developer_authorizations (developer_id, device_id) FROM stdin;
4	1
5	2
6	3
\.


--
-- Data for Name: device_requests; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.device_requests (client_id, is_requested, request_timestamp, requested_by_client_id) FROM stdin;
3	f	2020-09-02 12:31:27	\N
2	f	2020-10-07 17:33:00	5
1	f	2020-09-02 12:26:04	\N
\.


--
-- Name: clients clients_pk; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clients
    ADD CONSTRAINT clients_pk PRIMARY KEY (id);


--
-- Name: clients unique_client_name; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clients
    ADD CONSTRAINT unique_client_name UNIQUE (client_name);


--
-- Name: device_requests fk_client_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.device_requests
    ADD CONSTRAINT fk_client_id FOREIGN KEY (client_id) REFERENCES public.clients(id) MATCH FULL;


--
-- Name: client_connections fk_client_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.client_connections
    ADD CONSTRAINT fk_client_id FOREIGN KEY (client_id) REFERENCES public.clients(id) MATCH FULL;


--
-- Name: developer_authorizations fk_developer_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.developer_authorizations
    ADD CONSTRAINT fk_developer_id FOREIGN KEY (developer_id) REFERENCES public.clients(id) MATCH FULL ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: developer_authorizations fk_device_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.developer_authorizations
    ADD CONSTRAINT fk_device_id FOREIGN KEY (device_id) REFERENCES public.clients(id) MATCH FULL ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: device_requests fk_requester_client_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.device_requests
    ADD CONSTRAINT fk_requester_client_id FOREIGN KEY (requested_by_client_id) REFERENCES public.clients(id) MATCH FULL;


--
-- PostgreSQL database dump complete
--

