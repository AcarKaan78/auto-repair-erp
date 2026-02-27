; Bulent Oto Elektrik - Inno Setup Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isinfo.php)

#define MyAppName "Bulent Oto Elektrik Yonetim Sistemi"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Bulent Oto Elektrik"
#define MyAppExeName "BulentOtoElektrik.App.exe"
#define PublishDir "..\publish"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}

; Install to C:\BulentOtoElektrik (NOT Program Files - app writes data to BaseDirectory)
DefaultDirName=C:\BulentOtoElektrik
DisableDirPage=no

; Start Menu
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output installer
OutputDir=.\Output
OutputBaseFilename=BulentOtoElektrik_Kurulum_v{#MyAppVersion}
SetupIconFile=app.ico

; Compression
Compression=lzma2/ultra64
SolidCompression=yes

; Installer UI
WizardStyle=modern
WizardSizePercent=100

; Require admin privileges
PrivilegesRequired=admin

; Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

; 64-bit
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Version info
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}

; Upgrade: detect existing install location
UsePreviousAppDir=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaustune kisayol olustur"; GroupDescription: "Ek gorevler:"
Name: "startmenu"; Description: "Baslat menusune kisayol olustur"; GroupDescription: "Ek gorevler:"

[Files]
; All publish output EXCEPT user data files and debug symbols
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "bulentoto.db,*.pdb,export_settings.txt,logs\*,backups\*"

[Icons]
; Desktop shortcut
Name: "{userdesktop}\Bulent Oto Elektrik"; Filename: "{app}\{#MyAppExeName}"; \
  IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; Start Menu shortcuts
Name: "{userprograms}\{#MyAppName}\Bulent Oto Elektrik"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenu
Name: "{userprograms}\{#MyAppName}\Programi Kaldir"; Filename: "{uninstallexe}"; Tasks: startmenu

[Run]
; Option to launch after install
Filename: "{app}\{#MyAppExeName}"; Description: "Bulent Oto Elektrik'i baslat"; \
  Flags: nowait postinstall skipifsilent

[Code]
// Ask user about data preservation during uninstall
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataChoice: Integer;
  AppDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDir := ExpandConstant('{app}');

    if FileExists(AppDir + '\bulentoto.db') or
       DirExists(AppDir + '\backups') or
       DirExists(AppDir + '\logs') then
    begin
      DataChoice := MsgBox('Kullanici verileri (veritabani, yedekler, loglar) bulundu.' + #13#10 + #13#10 + 'Bu verileri silmek istiyor musunuz?' + #13#10 + #13#10 + '  EVET = Tum verileri sil' + #13#10 + '  HAYIR = Verileri koru (onerilen)', mbConfirmation, MB_YESNO);

      if DataChoice = IDYES then
      begin
        DelTree(AppDir + '\bulentoto.db', False, True, False);
        DelTree(AppDir + '\export_settings.txt', False, True, False);
        DelTree(AppDir + '\backups', True, True, True);
        DelTree(AppDir + '\logs', True, True, True);
        RemoveDir(AppDir);
      end;
    end;
  end;
end;
