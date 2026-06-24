# 📦 Como Gerar o Instalador (.exe) do SK MacroHelper

## Pré-requisitos

- .NET 8 SDK instalado
- Inno Setup 6 instalado → https://jrsoftware.org/isdl.php

---

## Passo 1 — Publicar o app em modo Release

Abra o terminal do VS Code na pasta `MacroHelper` e execute:

```bash
dotnet publish MacroHelper.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

Isso cria a pasta:
```
MacroHelper.UI\bin\Release\net8.0-windows\win-x64\publish\
```

---

## Passo 2 — Abrir o Inno Setup

1. Instale o Inno Setup 6: https://jrsoftware.org/isdl.php
2. Abra o Inno Setup Compiler
3. Vá em **File → Open** e selecione o arquivo `setup.iss` na raiz do projeto
4. Clique em **Build → Compile** (ou pressione `F9`)

---

## Passo 3 — Encontrar o instalador

O instalador será gerado em:
```
Installer\Output\SKMacroHelper_Setup_1.1.0.exe
```

---

## O que o instalador faz

✅ Instala o SK MacroHelper em `C:\Program Files\SKMacroHelper`  
✅ Cria atalho no Menu Iniciar  
✅ Opção de criar atalho na Área de Trabalho  
✅ Opção de iniciar automaticamente com o Windows  
✅ Inclui botão "Desinstalar" no Painel de Controle  
✅ Aparece com nome "SK MacroHelper — by Aline Martins · Silk"

---

## Para instalar em outro computador

Basta copiar o arquivo `SKMacroHelper_Setup_1.1.0.exe` para qualquer PC com Windows 10/11 e executar.

**Não precisa instalar o .NET 8** — o instalador já inclui tudo (--self-contained).
