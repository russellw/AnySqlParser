DROP DATABASE IF EXISTS test123987632022;
CREATE DATABASE test123987632022;
\c test123987632022

CREATE SEQUENCE public.items_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

create table items(
    id INT PRIMARY KEY DEFAULT nextval('public.items_id_seq'::regclass),
    name varchar(60)
);
