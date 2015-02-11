REM @echo off
setlocal

set exePath=c:\pd\DistributedTestRunner\TestRunController\bin\Debug
set curl=curl.exe
set serverUrl=http://localhost:6028

echo %exePath%

START %exePath%\testruncontroller.exe


%curl% %serverUrl%/testrun --header "Content-Type: text/plain" --data "TestsToBeDistributed.dll"

:CheckStatus
for /f "tokens=*" %%a in ('%curl% %serverUrl%/status') do (
    set CurlOutput=%%a
)

if "%CurlOutput%"=="{"isActive":false}" (
	echo SUCCESS
) else (
	timeout /t 5
    goto :CheckStatus
)

%curl% %serverUrl%/command --header "Content-Type: application/json" --data "\"shutdown\""

endlocal