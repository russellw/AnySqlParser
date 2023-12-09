@echo off
clang-format -i --style=file AnySqlParser\*.cs||exit /b
clang-format -i --style=file TestProject1\*.cs||exit /b
clang-format -i --style=file ConsoleApp1\*.cs||exit /b
clang-format -i --style=file Benchmark\*.cs||exit /b
call clean-cs -i -r .||exit /b
git diff
