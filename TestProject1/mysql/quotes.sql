DROP DATABASE IF EXISTS test123987632022;
CREATE DATABASE test123987632022;
USE test123987632022;

create table `test``table`(
    -- \ doesn't escape `
    `test``column` varchar(40) primary key
);

insert into `test``table` values('number''1');
-- \ does escape '
insert into `test``table` values('number\'2');
-- but not itself
-- this produces \\
insert into `test``table` values('number\\3');

select * from `test``table`;
