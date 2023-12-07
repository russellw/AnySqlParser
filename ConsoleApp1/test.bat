cd \AnySqlParser\ConsoleApp1||exit /b
msbuild /p:Configuration=Debug /p:Platform="Any CPU"||exit /b
echo off
for %%a in (\AnySqlParser\TestProject1\sql-server\*.sql) do "C:\AnySqlParser\ConsoleApp1\bin\Any CPU\Debug\net7.0\ConsoleApp1.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\sql-server-samples\*.sql) do "C:\AnySqlParser\ConsoleApp1\bin\Any CPU\Debug\net7.0\ConsoleApp1.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\northwind_psql\*.sql) do "C:\AnySqlParser\ConsoleApp1\bin\Any CPU\Debug\net7.0\ConsoleApp1.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\mysql\*.sql) do "C:\AnySqlParser\ConsoleApp1\bin\Any CPU\Debug\net7.0\ConsoleApp1.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\mysql-samples\*.sql) do "C:\AnySqlParser\ConsoleApp1\bin\Any CPU\Debug\net7.0\ConsoleApp1.exe" "%%a"||exit /b
