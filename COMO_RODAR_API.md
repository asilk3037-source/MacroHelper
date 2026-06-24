# 🌐 Como Rodar a API (para sincronização com mobile)

A API permite que o app mobile se conecte ao mesmo banco de macros.

## Rodar localmente (rede Wi-Fi)

```bash
cd MacroHelper.API
dotnet run
```

A API sobe em: `http://localhost:5000`

Para acessar do celular na mesma rede Wi-Fi:
1. Descubra o IP do seu PC: abra o CMD e digite `ipconfig`
2. Pegue o IPv4 (ex: `192.168.1.100`)
3. No app mobile, configure a URL como: `http://192.168.1.100:5000`

## Swagger (documentação interativa)

Acesse no navegador: http://localhost:5000/swagger

## Login padrão

- **E-mail:** admin@macrohelper.com
- **Senha:** admin123

## Para expor na internet (opcional)

Use ngrok para acesso externo:

```bash
ngrok http 5000
```

Copie a URL gerada (ex: https://abc123.ngrok.io) e configure no app mobile.

## Configurar chave da IA (opcional)

No arquivo `MacroHelper.API/appsettings.json`, adicione:

```json
"Anthropic": {
  "ApiKey": "sua-chave-aqui"
}
```

Obtenha sua chave em: https://console.anthropic.com
