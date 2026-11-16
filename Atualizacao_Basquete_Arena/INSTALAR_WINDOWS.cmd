@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALAR_ATUALIZACAO.ps1" %*
if errorlevel 1 echo Falha na instalacao. Confira a mensagem acima.
pause
