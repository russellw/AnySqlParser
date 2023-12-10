DROP DATABASE IF EXISTS test123987632022;
CREATE DATABASE test123987632022;
USE test123987632022;

create table Directors(
    Name varchar(40)
);

alter table directors add primary key(name);

create table movies(
    name varchar(40),
    year smallint check(1900<year and year<3000),
    director varchar(40),
    -- MySQL requires references fields to be explicitly named
    constraint movie_director foreign key(director) references directors(name)
);

insert into directors values('Stanley Kubrick');

insert into movies values('The Shining',1980,'Stanley Kubrick');

SELECT * FROM movies;
