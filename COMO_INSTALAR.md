# 📦 Guia Completo de Instalação — MacroHelper

## Passo 1 — Instalar o .NET 8 SDK

1. Abra o navegador e acesse:
   **https://dotnet.microsoft.com/download/dotnet/8.0**

2. Clique em **"SDK x64"** para Windows e baixe o instalador

3. Execute o arquivo baixado (`dotnet-sdk-8.x.x-win-x64.exe`)

4. Clique em **Instalar** e aguarde

5. **Confirme a instalação:** Abra o Prompt de Comando (`Win + R` → `cmd`)
   e digite:
   ```
   dotnet --version
   ```
   Deve aparecer algo como `8.0.xxx`

---

## Passo 2 — Instalar a extensão C# no VS Code

1. Abra o **VS Code**
2. Clique no ícone de **Extensões** (quadradinhos no lado esquerdo) ou `Ctrl+Shift+X`
3. Pesquise por: `C# Dev Kit`
4. Clique em **Instalar** na extensão da Microsoft

---

## Passo 3 — Abrir o projeto

1. No VS Code: **File → Open Folder...**
2. Navegue até a pasta **MacroHelper** (onde está este arquivo)
3. Clique em **Selecionar Pasta**

---

## Passo 4 — Executar o projeto

1. Abra o terminal no VS Code: **Terminal → New Terminal** (ou `Ctrl + '`)

2. No terminal, execute os comandos um por vez:

   ```bash
   dotnet restore
   ```
   *(baixa as dependências — pode demorar um pouco na primeira vez)*

   ```bash
   dotnet run --project MacroHelper.UI
   ```
   *(compila e abre o aplicativo)*

3. O **MacroHelper** será aberto! 🎉

---

## ❓ Solução de Problemas

### "dotnet: command not found"
→ O .NET 8 SDK não foi instalado corretamente. Repita o Passo 1 e reinicie o VS Code.

### Erros de compilação
→ No terminal, execute:
```bash
dotnet restore
dotnet build
```
Se ainda houver erros, copie a mensagem de erro e me envie para eu ajudar.

### A janela não abre
→ Certifique-se de estar executando `dotnet run --project MacroHelper.UI`, não apenas `dotnet run`.

---

## Como usar após abrir

| Ação | Como fazer |
|------|-----------|
| Criar macro | Aba "Macros" → botão "Nova Macro" |
| Editar macro | Clique no ícone ✏ da macro |
| Excluir macro | Clique no ícone 🗑 da macro |
| Copiar conteúdo | Clique no ícone 📋 da macro |
| Mudar tema | Aba "Configurações" → escolha o tema |
| Usar macro | Em qualquer campo: `/atalho` + aguarde popup |
| Inserir macro | Pressione `Enter` ou clique na sugestão |
| Fechar popup | Pressione `Esc` |
