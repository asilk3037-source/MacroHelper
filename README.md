# 🚀 SK MacroHelper

> **Ferramenta de produtividade para inserção rápida de textos padronizados**
> Desenvolvido por **Aline Martins · Silk**

---

## 📦 O que está incluído

| Projeto | Tecnologia | Descrição |
|---------|-----------|-----------|
| `MacroHelper.UI` | WPF · .NET 8 | App desktop Windows |
| `MacroHelper.API` | ASP.NET Core 8 | API REST para sincronização mobile |
| `MacroHelper.Mobile` | .NET MAUI 8 | App Android + iOS |
| `MacroHelper.Core` | .NET 8 | Entidades e interfaces |
| `MacroHelper.Data` | SQLite + Dapper | Repositórios |
| `MacroHelper.Services` | .NET 8 | Lógica de negócio + IA |

---

## ▶️ Rodar o desktop (Windows)

```bash
dotnet restore
dotnet run --project MacroHelper.UI
```

**Login padrão:** `admin@macrohelper.com` / `admin123`

---

## 🌐 Rodar a API (sincronização mobile)

```bash
dotnet run --project MacroHelper.API
```

Acesse o Swagger em: http://localhost:5000/swagger

---

## 📱 Rodar o app mobile

```bash
dotnet build MacroHelper.Mobile -t:Run -f net8.0-android
```

Configure a URL da API nas Configurações do app mobile.

---

## 📦 Gerar instalador Windows

```bash
dotnet publish MacroHelper.UI -c Release -r win-x64 --self-contained true
```

Depois abrir `setup.iss` no Inno Setup 6 e compilar.
Veja o guia completo em `COMO_GERAR_INSTALADOR.md`.

---

## ✨ Funcionalidades

- ✅ Cadastro de macros com categorias e subcategorias
- ✅ Hook global de teclado (PT-BR/ABNT2)
- ✅ Popup flutuante em qualquer app Windows
- ✅ Login por usuário com perfis Admin/Usuário
- ✅ Relatório de uso com exportação CSV
- ✅ Temas Dark / Light / Sistema
- ✅ IA: gerar conteúdo por descrição
- ✅ IA: ajustar tom (formal, informal, curto, empático)
- ✅ API REST com autenticação JWT
- ✅ App mobile Android + iOS

---

## 🔑 Configurar IA (opcional)

No arquivo `MacroHelper.API/appsettings.json`:
```json
"Anthropic": { "ApiKey": "sua-chave-aqui" }
```

Obtenha a chave em: https://console.anthropic.com

---

*SK MacroHelper v1.1.0 · Junho 2026*
*Desenvolvido com ❤️ por Aline Martins · Silk*
