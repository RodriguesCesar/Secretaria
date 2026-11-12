# Secretaria API

API ASP.NET para atendimento da secretaria com integração ao Azure AI Projects.

## Configuração

1. Ajuste o arquivo de configuração:
   - [appsettings.json](./Secretaria.api/appsettings.json)
   - (opcional local) [appsettings.Development.json](./Secretaria.api/appsettings.Development.json)

2. Configure `Agent`:
   - `ProjectEndpoint`
   - `AgentName`
   - `AgentVersion`

3. Configure CORS para produção em `Cors:AllowedOrigins`.

## Execução local

```bash
dotnet run --project Secretaria.api/Secretaria.api.csproj
```

## Endpoints

- `POST /api/atendimento`
- `GET /health`

## Melhorias implementadas

- validação de entrada com DataAnnotations no request de atendimento;
- tratamento global de exceções com `ProblemDetails` e `traceId`;
- health check (`/health`);
- CORS restrito por ambiente (aberto apenas em Development);
- validação de configurações críticas (`AgentSettings`) no startup;
- redirecionamento HTTPS/HSTS em ambientes não Development;
- pipeline de CI com restore, build e test em [ci.yml](./.github/workflows/ci.yml).
