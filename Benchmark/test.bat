cd \AnySqlParser\Benchmark||exit /b
msbuild /p:Configuration=Debug /p:Platform="Any CPU"||exit /b
echo off
for %%a in (\AnySqlParser\TestProject1\sql-server\*.sql) do "C:\AnySqlParser\Benchmark\bin\Any CPU\Debug\net7.0\Benchmark.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\sql-server-samples\*.sql) do "C:\AnySqlParser\Benchmark\bin\Any CPU\Debug\net7.0\Benchmark.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\northwind_psql\*.sql) do "C:\AnySqlParser\Benchmark\bin\Any CPU\Debug\net7.0\Benchmark.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\mysql\*.sql) do "C:\AnySqlParser\Benchmark\bin\Any CPU\Debug\net7.0\Benchmark.exe" "%%a"||exit /b
for %%a in (\AnySqlParser\TestProject1\mysql-samples\*.sql) do "C:\AnySqlParser\Benchmark\bin\Any CPU\Debug\net7.0\Benchmark.exe" "%%a"||exit /b
