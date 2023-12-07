PRAGMA foreign_keys=ON;

create table directors(
    name varchar(40) primary key
);

create table movies(
    name varchar(40),
    year smallint check(1900<year and year<3000),
    director varchar(40),
    constraint movie_director foreign key(director) references directors
);

insert into directors values('Stanley Kubrick');

insert into movies values('The Shining',1980,'Stanley Kubrick');

SELECT * FROM movies;
