DROP DATABASE IF EXISTS test123987632022;
CREATE DATABASE test123987632022;
USE test123987632022;

-- ` works, " doesn't
-- \ doesn't escape `
-- names can contain newline
create table `test``table`(
    `test
column` varchar(40) primary key
);

insert into `test``table` values('number''1');

-- \ does escape '
--TODO
--insert into `test``table` values('number\'2');

-- but not itself
-- this produces \\
insert into `test``table` values('number\\3');

-- string literal can contain newline
insert into `test``table` values('number
4');

select * from `test``table`;
