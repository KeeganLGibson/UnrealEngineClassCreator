; UE Class Creator — Inno Setup 6 installer script
; https://jrsoftware.org/isinfo.php
;
; To build:
;   iscc UEClassCreator.iss          (command line)
;   or open in Inno Setup IDE and press Compile (Ctrl+F9)
;
; Output: Installer\Output\UEClassCreator-Setup-{version}.exe
;
; IMPORTANT: Keep AppVersion in sync with <Version> in UEClassCreator.csproj.
; The AppId GUID must never change — it is how Windows identifies this app
; for upgrades and uninstallation.

#define AppName      "UE Class Creator"
#define AppVersion   "0.1.4"
#define AppPublisher "Keegan Gibson"
#define AppExeName   "UEClassCreator.exe"
#define SourceDir    "..\UEClassCreator\bin\Publish\win-x64"

[Setup]
AppId={{4B7E2A1F-9C3D-4F8A-B6E5-2D1C3A4F5B6E}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}

; Install to per-user AppData by default (no UAC prompt).
; The dialog option lets the user choose to install for all users (requires UAC).
DefaultDirName={autopf}\UEClassCreator
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

OutputDir=Output
OutputBaseFilename=UEClassCreator-Setup-{#AppVersion}

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

; 64-bit only — matches the win-x64 publish profile
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

MinVersion=10.0

; Close the running app automatically before upgrading
CloseApplications=yes
CloseApplicationsFilter={#AppExeName}
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main executable
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Mustache templates — must ship alongside the exe
Source: "{#SourceDir}\Templates\*"; DestDir: "{app}\Templates"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";                       Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";                 Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "{cm:LaunchProgram,{#AppName}}"; \
  Flags: nowait postinstall skipifsilent
