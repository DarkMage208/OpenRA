; Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
; This file is part of OpenRA.
; 
;  OpenRA is free software: you can redistribute it and/or modify
;  it under the terms of the GNU General Public License as published by
;  the Free Software Foundation, either version 3 of the License, or
;  (at your option) any later version.
; 
;  OpenRA is distributed in the hope that it will be useful,
;  but WITHOUT ANY WARRANTY; without even the implied warranty of
;  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;  GNU General Public License for more details.
; 
;  You should have received a copy of the GNU General Public License
;  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.


!include "MUI2.nsh"
!include "ZipDLL.nsh"

Name "OpenRA"
OutFile "OpenRA.exe"

InstallDir $PROGRAMFILES\OpenRA
SetCompressor lzma

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\COPYING"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\OpenRA"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "OpenRA"

Var StartMenuFolder
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder

!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
;!insertmacro MUI_PAGE_FINISH

;!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;***************************
;Section Definitions
;***************************
Section "Client" Client
	SetOutPath "$INSTDIR"
	File "..\..\OpenRA.Game\bin\Debug\OpenRA.Game.exe"
	File "..\..\OpenRA.Game\bin\Debug\OpenRA.FileFormats.dll"
	File "..\..\OpenRA.Gl.dll"
	File "..\..\COPYING"
	File "..\..\HACKING"
	File "..\..\INSTALL"
	File "..\..\settings-netplay-cnc.ini"
	File "..\..\settings-netplay-ra.ini"
	File "..\..\FreeSans.ttf"
	File "..\..\FreeSansBold.ttf"
	
	File "..\..\OpenRA.Game\OpenRA.ico"
	
	File "..\..\thirdparty\Tao\*.dll"
	
	File "cg.dll"
	File "cgGL.dll"
	
	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\OpenRA.lnk" $OUTDIR\OpenRA.Game.exe "" \
			"$OUTDIR\OpenRA.ico" "" "" "" "OpenRA Client"
	!insertmacro MUI_STARTMENU_WRITE_END
	
	SetOutPath "$INSTDIR\shaders"
	File "..\..\shaders\*.fx"
SectionEnd

SectionGroup /e "Mods"
	SectionGroup "Red Alert" RA
		Section "-RA_Core"
			SetOutPath "$INSTDIR\mods\ra"
			File "..\..\mods\ra\*.*"
		SectionEnd
		Section "Download content" RA_Content
			AddSize 10137
			SetOutPath "$OUTDIR\packages"
			NSISdl::download http://open-ra.org/packages/ra-packages.zip ra-packages.zip
			Pop $R0
			StrCmp $R0 "success" +2
				Abort
			ZipDLL::extractall "ra-packages.zip" "$OUTDIR"
			Delete ra-packages.zip
		SectionEnd
	SectionGroupEnd
	SectionGroup "Command & Conquer" CNC
		Section "-CNC_Core"
			SetOutPath "$INSTDIR\mods\cnc"
			File "..\..\mods\cnc\*.*"
		SectionEnd
		Section "Download content" CNC_Content
			AddSize 9431
			SetOutPath "$OUTDIR\packages"
			NSISdl::download http://open-ra.org/packages/cnc-packages.zip cnc-packages.zip
			Pop $R0
			StrCmp $R0 "success" +2
				Abort
			ZipDLL::extractall "cnc-packages.zip" "$OUTDIR"
			Delete cnc-packages.zip
		SectionEnd
	SectionGroupEnd
	SectionGroup "Red Alert: Aftermath" Aftermath
		Section "-AM_Core"
			SetOutPath "$INSTDIR\mods\aftermath"
			File "..\..\mods\aftermath\*.*"
		SectionEnd
		Section "Download content" AM_Content
			AddSize 5941
			SetOutPath "$OUTDIR\packages"
			NSISdl::download http://open-ra.org/packages/aftermath-packages.zip aftermath-packages.zip
			Pop $R0
			StrCmp $R0 "success" +2
				Abort
			ZipDLL::extractall "aftermath-packages.zip" "$OUTDIR"
			Delete aftermath-packages.zip
		SectionEnd
	SectionGroupEnd
	Section "Red Alert: Next Generation" RA_NG
		SetOutPath "$INSTDIR\mods\ra-ng"
		File "..\..\mods\ra-ng\*.*"
	SectionEnd
SectionGroupEnd

;***************************
;Dependency Sections
;***************************
Section "-OpenAl" OpenAl
	AddSize 768
	IfFileExists $SYSDIR\OpenAL32.dll done installal
	installal:
		SetOutPath "$TEMP"
		NSISdl::download http://connect.creativelabs.com/openal/Downloads/oalinst.zip oalinst.zip
		Pop $R0
		StrCmp $R0 "success" +2
			Abort
		!insertmacro ZIPDLL_EXTRACT oalinst.zip OpenAL oalinst.exe
		ExecWait "$TEMP\OpenAL\oalinst.exe"
	done:
SectionEnd

Section "-Sdl" SDL
	AddSize 317
	SetOutPath "$TEMP"
	NSISdl::download http://www.libsdl.org/release/SDL-1.2.14-win32.zip sdl.zip
	!insertmacro ZIPDLL_EXTRACT sdl.zip $INSTDIR SDL.dll
SectionEnd

Section "-Freetype" Freetype
	AddSize 583
	SetOutPath "$TEMP"
	NSISdl::download http://www.open-ra.org/releases/windows/freetype-zlib.zip freetype-zlib.zip
	Pop $R0
	StrCmp $R0 "success" +2
		Abort
	ZipDLL::extractall "freetype-zlib.zip" "$INSTDIR"
SectionEnd

;***************************
;Uninstaller Sections
;***************************
Section "-Uninstaller"
	WriteUninstaller $INSTDIR\uninstaller.exe
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "DisplayName" "OpenRA"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "UninstallString" "$INSTDIR\uninstaller.exe"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "InstallLocation" "$INSTDIR"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "DisplayIcon" "$INSTDIR\OpenRA.ico"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "Publisher" "IJW Software (New Zealand)"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "URLInfoAbout" "http://open-ra.org"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "NoModify" "1"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "NoRepair" "1"
	
	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Uninstall.lnk" "$INSTDIR\uninstaller.exe" "" \
			"" "" "" "" "Uninstall OpenRA"
	!insertmacro MUI_STARTMENU_WRITE_END
SectionEnd

!macro Clean UN
Function ${UN}Clean
	RMDir /r $INSTDIR\mods
	RMDir /r $INSTDIR\shaders
	Delete $INSTDIR\OpenRA.Game.exe
	Delete $INSTDIR\OpenRA.FileFormats.dll
	Delete $INSTDIR\OpenRA.Gl.dll
	Delete $INSTDIR\Tao.*.dll
	Delete $INSTDIR\COPYING
	Delete $INSTDIR\HACKING
	Delete $INSTDIR\INSTALL
	Delete $INSTDIR\OpenRA.ico
	Delete $INSTDIR\*.ttf
	Delete $INSTDIR\settings-netplay-*.ini
	Delete $INSTDIR\freetype6.dll
	Delete $INSTDIR\SDL.dll
	Delete $INSTDIR\cg.dll
	Delete $INSTDIR\cgGL.dll
	Delete $INSTDIR\zlib1.dll
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA"
	Delete $INSTDIR\uninstaller.exe
	RMDir $INSTDIR
	
	!insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder
	RMDir /r "$SMPROGRAMS\$StartMenuFolder"
	DeleteRegKey HKCU "Software\OpenRA"
FunctionEnd
!macroend

!insertmacro Clean ""
!insertmacro Clean "un."

Section "Uninstall"
	Call un.Clean
SectionEnd

;***************************
;Section Descriptions
;***************************
LangString DESC_Client ${LANG_ENGLISH} "OpenRA client and dependencies"
LangString DESC_RA ${LANG_ENGLISH} "Base Red Alert mod"
LangString DESC_CNC ${LANG_ENGLISH} "Base Command and Conquer mod"
LangString DESC_Aftermath ${LANG_ENGLISH} "Red Alert: Aftermath expansion mod (depends on base Red Alert mod)"
LangString DESC_RA_NG ${LANG_ENGLISH} "Next-gen Red Alert mod (depends on base Red Alert mod)"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Client} $(DESC_Client)
	!insertmacro MUI_DESCRIPTION_TEXT ${RA} $(DESC_RA)
	!insertmacro MUI_DESCRIPTION_TEXT ${CNC} $(DESC_CNC)
	!insertmacro MUI_DESCRIPTION_TEXT ${Aftermath} $(DESC_Aftermath)
	!insertmacro MUI_DESCRIPTION_TEXT ${RA_NG} $(DESC_RA_NG)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;***************************
;Callbacks
;***************************

Function .onInstFailed
	Call Clean
FunctionEnd
