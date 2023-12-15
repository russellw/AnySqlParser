@echo off
call clang-format-all
call clean-cs -i -r .||exit /b
call clang-format-all
git diff
