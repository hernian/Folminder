#define MyAppVersion "1.0.0"

[Setup]
AppId={{C1818027-ECCB-4994-8151-CE3EA752EAFC}
AppName=Folminder
AppVersion={#MyAppVersion}
AppPublisher=Hernian (hernianrunner@gmail.com)
AppPublisherURL=https://github.com/hernian/Folminder
VersionInfoVersion={#MyAppVersion}
DefaultDirName={localappdata}\Hernian\Folminder
DefaultGroupName=Folminder
OutputDir=Output
OutputBaseFilename=folminder_setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\Folminder.exe
LicenseFile=..\LICENSE.txt
WizardStyle=modern
SetupIconFile=folminder.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\Folminder\bin\Release\net10.0-windows\*"; \
        DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Folminder"; Filename: "{app}\Folminder.exe"
Name: "{userstartup}\Folminder"; Filename: "{app}\Folminder.exe"; Tasks: startupicon

[Tasks]
Name: "startupicon"; Description: "Start Folminder at &Windows startup"; GroupDescription: "Additional icons:"

[InstallDelete]
; 古いバージョンのファイルを削除
Type: filesandordirs; Name: "{app}\*"

[UninstallDelete]
; ユーザー設定ファイルを削除するか確認
Type: filesandordirs; Name: "{localappdata}\Folminder"

[Run]
Filename: "{app}\Folminder.exe"; Description: "Launch Folminder"; Flags: nowait postinstall skipifsilent
