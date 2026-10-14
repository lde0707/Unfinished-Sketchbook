@echo off
chcp 65001 >nul
set PYTHONIOENCODING=utf-8
cd /d "%~dp0.."
echo [Unfin] 아트 자동 검증: Art\inbox 에 그림을 넣으면 자동으로 검사합니다 (파일 이름 = 에셋 ID)
python -m pip install --quiet numpy pillow scipy
python Tools\art_check.py watch
pause
