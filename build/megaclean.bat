@ECHO OFF

REM megaclean
REM ZBRIDGE MEGACLEAN
REM
REM Performs a complete clean of the source tree, leaving only
REM the files that should be checked into source control
REM

CD ..

REM ***
REM MAKE EVERYTHING READ/WRITE
REM ***

ATTRIB *.* -S -H -R /S
ATTRIB *.* -S -H -R /S /D

REM ***
REM REMOVE/RECREATE OUTPUT DIRECTORIES
REM ***

IF EXIST .\bin RD /S /Q .\bin
IF EXIST .\build\logs RD /S /Q .\build\logs
IF EXIST .\setup RD /S /Q .\setup
IF EXIST .\ipch RD /S /Q .\ipch
IF EXIST .\tmp RD /S /Q .\tmp /S
IF EXIST *.sdf DEL *.sdf
IF EXIST *.suo DEL *.suo
IF EXIST .\build\tools\*.sdf DEL .\build\tools\*.sdf
IF EXIST .\build\tools\*.suo DEL .\build\tools\*.suo

REM ***
REM CLEAN PROJECT .USER FILES AND VSHOST FILES
REM ***

DEL *.VBPROJ.USER /S
DEL *.VCXPROJ.USER /S
DEL *.CSPROJ.USER /S
DEL *.VSHOST.EXE /S
DEL *.VSHOST.EXE.MANIFEST /S

REM ***
REM CLEAN EMBEDDED INTERMEDIATE FOLDERS
REM ***

FOR /D %%i IN (src\*.*) DO IF EXIST %%i\obj RD /S /Q .\%%i\obj
FOR /D %%i IN (src\*.*) DO IF EXIST %%i\bin RD /S /Q .\%%i\bin
FOR /D %%i IN (src\*.*) DO IF EXIST %%i\Itanium RD /S /Q .\%%i\Itanium
FOR /D %%i IN (src\*.*) DO IF EXIST %%i\Win32 RD /S /Q .\%%i\Win32
FOR /D %%i IN (src\*.*) DO IF EXIST %%i\x64 RD /S /Q .\%%i\x64

FOR /D %%i IN (src\web\*.*) DO IF EXIST %%i\obj RD /S /Q .\%%i\obj
FOR /D %%i IN (src\web\*.*) DO IF EXIST %%i\bin RD /S /Q .\%%i\bin
FOR /D %%i IN (src\web\*.*) DO IF EXIST %%i\Itanium RD /S /Q .\%%i\Itanium
FOR /D %%i IN (src\web\*.*) DO IF EXIST %%i\Win32 RD /S /Q .\%%i\Win32
FOR /D %%i IN (src\web\*.*) DO IF EXIST %%i\x64 RD /S /Q .\%%i\x64

FOR /D %%i IN (build\tools\src\*.*) DO IF EXIST %%i\obj RD /S /Q .\%%i\obj
FOR /D %%i IN (build\tools\src\*.*) DO IF EXIST %%i\bin RD /S /Q .\%%i\bin
FOR /D %%i IN (build\tools\src\*.*) DO IF EXIST %%i\Itanium RD /S /Q .\%%i\Itanium
FOR /D %%i IN (build\tools\src\*.*) DO IF EXIST %%i\Win32 RD /S /Q .\%%i\Win32
FOR /D %%i IN (build\tools\src\*.*) DO IF EXIST %%i\x64 RD /S /Q .\%%i\x64




