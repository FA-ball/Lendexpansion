@echo off
echo 正在编译 QuestNode_LetterByGender.dll ...
set "csc=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%csc%" set "csc=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%csc%" /target:library /out:QuestNode_LetterByGender.dll /reference:"Assembly-CSharp.dll" /reference:"UnityEngine.dll" /reference:"UnityEngine.CoreModule.dll" QuestNode_LetterByGender.cs
if errorlevel 1 (
    echo 编译失败！请确保 RimWorld 的托管 DLL 在当前目录。
    pause
    exit /b 1
)
echo 编译成功！请将生成的 DLL 放入 Mod 的 Assemblies 文件夹。
pause