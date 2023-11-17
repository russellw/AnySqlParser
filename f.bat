@echo off
clang-format -i --style=file AnySqlParser\*.cs||exit /b
clang-format -i --style=file TestProject1\*.cs||exit /b
git diff
