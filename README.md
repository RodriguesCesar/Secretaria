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

4. Configure resiliência da chamada ao agente em `AtendimentoResilience`:
   - `TimeoutSeconds`
   - `MaxRetries`
   - `RetryDelayMilliseconds`

## Execução local

```bash
dotnet run --project Secretaria.api/Secretaria.api.csproj
```

## Endpoints

- `POST /api/atendimento`
- `GET /health`

## Testes

```bash
dotnet test Secretaria.slnx
```

Projeto de testes:

- `Secretaria.api.Tests`

## Melhorias implementadas

- validação de entrada com DataAnnotations no request de atendimento;
- tratamento global de exceções com `ProblemDetails` e `traceId`;
- health check (`/health`);
- CORS restrito por ambiente (aberto apenas em Development);
- validação de configurações críticas (`AgentSettings`) no startup;
- redirecionamento HTTPS/HSTS em ambientes não Development;
- camada de cliente para Azure AI desacoplada do serviço de domínio;
- política de resiliência com timeout e retry configuráveis;
- suíte inicial de testes (integração e unitários);
- pipeline de CI com restore, build e test em [ci.yml](./.github/workflows/ci.yml).
