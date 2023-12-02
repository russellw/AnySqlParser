DROP DATABASE IF EXISTS test123987632022
CREATE DATABASE test123987632022
use test123987632022

create table countries(
    code nchar(2) primary key,
    name nvarchar(60) unique
)
insert countries values('DE','Germany')
insert countries values('FR','France')
insert countries values('GB','United Kingdom')
insert countries values('IE','Ireland')
insert countries values('IT','Italy')
insert countries values('JP','Japan')
insert countries values('US','United States')

create table cities(
    id int identity,
    country nchar(2) foreign key references countries,
    name nvarchar(60)
)
insert cities values('DE','Berlin')
insert cities values('FR','Le Havre')
insert cities values('FR','Paris')
insert cities values('GB','Birmingham')
insert cities values('GB','Glasgow')
insert cities values('GB','Liverpool')
insert cities values('GB','London')
insert cities values('IE','Cork')
insert cities values('IE','Dublin')
insert cities values('IE','Galway')
insert cities values('IT','Milan')
insert cities values('IT','Rome')
insert cities values('IT','Venice')
insert cities values('JP','Kyoto')
insert cities values('JP','Tokyo')
insert cities values('US','Chicago')
insert cities values('US','Los Angeles')
insert cities values('US','New York')
insert cities values('US','San Francisco')

SELECT * FROM countries
SELECT * FROM cities
go
