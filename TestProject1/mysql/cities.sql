DROP DATABASE IF EXISTS test123987632022;
CREATE DATABASE test123987632022;
use test123987632022;

create table countries(
    code char(2) primary key,
    name varchar(60) unique
);
insert countries values('DE','Germany');
insert countries values('FR','France');
insert countries values('GB','United Kingdom');
insert countries values('IE','Ireland');
insert countries values('IT','Italy');
insert countries values('JP','Japan');
insert countries values('US','United States');

create table cities(
    id int AUTO_INCREMENT PRIMARY KEY,
    country char(2),
    name varchar(60),
    foreign key (country) references countries(code)
);
insert cities(country,name) values('DE','Berlin');
insert cities(country,name) values('FR','Le Havre');
insert cities(country,name) values('FR','Paris');
insert cities(country,name) values('GB','Birmingham');
insert cities(country,name) values('GB','Glasgow');
insert cities(country,name) values('GB','Liverpool');
insert cities(country,name) values('GB','London');
insert cities(country,name) values('IE','Cork');
insert cities(country,name) values('IE','Dublin');
insert cities(country,name) values('IE','Galway');
insert cities(country,name) values('IT','Milan');
insert cities(country,name) values('IT','Rome');
insert cities(country,name) values('IT','Venice');
insert cities(country,name) values('JP','Kyoto');
insert cities(country,name) values('JP','Tokyo');
insert cities(country,name) values('US','Chicago');
insert cities(country,name) values('US','Los Angeles');
insert cities(country,name) values('US','New York');
insert cities(country,name) values('US','San Francisco');

SELECT * FROM countries;
select '';
SELECT * FROM cities;
