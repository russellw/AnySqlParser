DROP DATABASE IF EXISTS test123987632022;
CREATE DATABASE test123987632022;
\c test123987632022

create table countries(
    code char(2) primary key,
    name varchar(60) unique
);
insert into countries values('DE','Germany');
insert into countries values('FR','France');
insert into countries values('GB','United Kingdom');
insert into countries values('IE','Ireland');
insert into countries values('IT','Italy');
insert into countries values('JP','Japan');
insert into countries values('US','United States');

create table cities(
    id serial PRIMARY KEY,
    country char(2),
    name varchar(60),
    foreign key (country) references countries(code)
);
insert into cities(country,name) values('DE','Berlin');
insert into cities(country,name) values('FR','Le Havre');
insert into cities(country,name) values('FR','Paris');
insert into cities(country,name) values('GB','Birmingham');
insert into cities(country,name) values('GB','Glasgow');
insert into cities(country,name) values('GB','Liverpool');
insert into cities(country,name) values('GB','London');
insert into cities(country,name) values('IE','Cork');
insert into cities(country,name) values('IE','Dublin');
insert into cities(country,name) values('IE','Galway');
insert into cities(country,name) values('IT','Milan');
insert into cities(country,name) values('IT','Rome');
insert into cities(country,name) values('IT','Venice');
insert into cities(country,name) values('JP','Kyoto');
insert into cities(country,name) values('JP','Tokyo');
insert into cities(country,name) values('US','Chicago');
insert into cities(country,name) values('US','Los Angeles');
insert into cities(country,name) values('US','New York');
insert into cities(country,name) values('US','San Francisco');

SELECT * FROM countries;
select '';
SELECT * FROM cities;
