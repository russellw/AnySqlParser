-- \ doesn't escape "
-- names can contain newline
create table "test""table"(
    "test
column" varchar(40) primary key
);

insert into "test""table" values('number''1');

-- \ does not escape '

-- \ does not escape itself
-- this produces \\
insert into "test""table" values('number\\3');

-- string literal can contain newline
insert into "test""table" values('number
4');

select * from "test""table";
