; ─────────────────────────────────────────────
;  SK MacroHelper — Inno Setup Script
;  Desenvolvido por Aline Martins · Silk
; ─────────────────────────────────────────────

#define MyAppName      "SK MacroHelper"
#define MyAppVersion   "1.1.0"
#define MyAppPublisher "Silk · Aline Martins"
#define MyAppExeName   "MacroHelper.exe"
#define BuildDir       "MacroHelper.UI\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{B8D2F3A1-C4E5-4F6A-A7B8-C9D0E1F2A3B4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SKMacroHelper
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Installer\Output
OutputBaseFilename=SKMacroHelper_Setup_{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=SK MacroHelper — Ferramenta de produtividade por Aline Martins · Silk

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon";  Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos:";
Name: "startupicon";  Description: "Iniciar automaticamente com o Windows"; GroupDescription: "Opções:";

[Files]
Source: "{#BuildDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\*.dll";           DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "{#BuildDir}\*.json";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\runtimes\*";      DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}";            Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";      Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{commonstartup}\{#MyAppName}";    Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir SK MacroHelper agora"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SKMacroHelper"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Messages]
BeveledLabel=SK MacroHelper {#MyAppVersion} — by Aline Martins · Silk
