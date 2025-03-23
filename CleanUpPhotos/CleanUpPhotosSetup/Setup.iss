[Setup]
AppName=Clean Up Photos
AppId=CleanUpPhotos
AppVerName=Clean Up Photos 3.1.3
AppCopyright=Copyright © Doena Soft. 2011 - 2025
AppPublisher=Doena Soft.
AppPublisherURL=http://doena-journal.net/en/dvd-profiler-tools/
DefaultDirName={commonpf32}\Doena Soft.\Clean Up Photos
; DefaultGroupName=Doena Soft.
DirExistsWarning=No
SourceDir=..\CleanUpPhotos\bin\x86\Release\net472
Compression=zip/9
AppMutex=InvelosDVDPro
OutputBaseFilename=CleanUpPhotosSetup
OutputDir=..\..\..\..\..\CleanUpPhotosSetup\Setup\CleanUpPhotos
MinVersion=0,6.1sp1
PrivilegesRequired=admin
WizardStyle=modern
DisableReadyPage=yes
ShowLanguageDialog=no
VersionInfoCompany=Doena Soft.
VersionInfoCopyright=2011 - 2025
VersionInfoDescription=Clean Up Photos Setup
VersionInfoVersion=3.1.3
UninstallDisplayIcon={app}\djdsoft.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WinVersionTooLowError=This program requires Windows XP or above to be installed.%n%nWindows 9x, NT and 2000 are not supported.

[Types]
Name: "full"; Description: "Full installation"

[Files]
Source: "djdsoft.ico"; DestDir: "{app}"; Flags: ignoreversion

Source: "*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion


Source: "de\*.dll"; DestDir: "{app}\de"; Flags: ignoreversion

Source: "Readme\readme.html"; DestDir: "{app}\Readme"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Run]
Filename: "{win}\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"; Parameters: "/codebase ""{app}\DoenaSoft.CleanUpPhotos.dll"""; Flags: runhidden

;[UninstallDelete]

[UninstallRun]
Filename: "{win}\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"; Parameters: "/u ""{app}\DoenaSoft.CleanUpPhotos.dll"""; Flags: runhidden

[Registry]
; Register - Cleanup ahead of time in case the user didn't uninstall the previous version.
Root: HKCR; Subkey: "CLSID\{{447536AA-253C-4626-B5D0-C543492CC3B2}"; Flags: dontcreatekey deletekey
Root: HKCR; Subkey: "DoenaSoft.DVDProfiler.CleanUpPhotos.Plugin"; Flags: dontcreatekey deletekey
Root: HKCU; Subkey: "Software\Invelos Software\DVD Profiler\Plugins\Identified"; ValueType: none; ValueName: "{{447536AA-253C-4626-B5D0-C543492CC3B2}"; ValueData: "0"; Flags: deletevalue
Root: HKLM; Subkey: "Software\Classes\CLSID\{{447536AA-253C-4626-B5D0-C543492CC3B2}"; Flags: dontcreatekey deletekey
Root: HKLM; Subkey: "Software\Classes\DoenaSoft.DVDProfiler.CleanUpPhotos.Plugin"; Flags: dontcreatekey deletekey
; Unregister
Root: HKCR; Subkey: "CLSID\{{447536AA-253C-4626-B5D0-C543492CC3B2}"; Flags: dontcreatekey uninsdeletekey
Root: HKCR; Subkey: "DoenaSoft.DVDProfiler.CleanUpPhotos.Plugin"; Flags: dontcreatekey uninsdeletekey
Root: HKCU; Subkey: "Software\Invelos Software\DVD Profiler\Plugins\Identified"; ValueType: none; ValueName: "{{447536AA-253C-4626-B5D0-C543492CC3B2}"; ValueData: "0"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\CLSID\{{447536AA-253C-4626-B5D0-C543492CC3B2}"; Flags: dontcreatekey uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\DoenaSoft.DVDProfiler.CleanUpPhotos.Plugin"; Flags: dontcreatekey uninsdeletekey

[Code]
function IsDotNET4Detected(): boolean;
// Function to detect dotNet framework version 4
// Returns true if it is available, false it's not.
var
dotNetStatus: boolean;
begin
dotNetStatus := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4');
Result := dotNetStatus;
end;

function InitializeSetup(): Boolean;
// Called at the beginning of the setup package.
begin

if not IsDotNET4Detected then
begin
MsgBox( 'The Microsoft .NET Framework version 4 is not installed. Please install it and try again.', mbInformation, MB_OK );
Result := false;
end
else
Result := true;
end;