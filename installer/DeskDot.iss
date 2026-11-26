; DeskDot installer — build with Inno Setup 6 (https://jrsoftware.org/isinfo.php)
;   ISCC.exe installer\DeskDot.iss /DAppVersion=2.0.0
; Expects a published, self-contained build in ..\publish\win-x64

#define AppName        "DeskDot"
#define AppPublisher   "D.R.M.Y."
#define AppExeName     "DeskDot.exe"
#define AppUrl         "https://github.com/Maheshyadav1318/DeskDot"

#ifndef AppVersion
  #define AppVersion "2.0.0"
#endif

[Setup]
AppId={{8F3C2B71-4D5A-4E8C-9B21-7A6F5C4D3E10}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={autopf}\{#AppName}
; PrivilegesRequired=lowest routes {autopf} to %LOCALAPPDATA%\Programs ({userpf})
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=DeskDot-{#AppVersion}-setup
SetupIconFile=..\src\DeskDot\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start DeskDot when I sign in"; GroupDescription: "Startup:"; Flags: unchecked
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{#AppName} Setup"; Filename: "{app}\{#AppExeName}"; Parameters: "--settings"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Parameters: "--tray"; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch DeskDot"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
