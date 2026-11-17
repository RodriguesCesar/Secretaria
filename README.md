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

5. Configure autenticação e rate limiting do endpoint de atendimento:
   - `Authentication:AtendimentoApiKeys` (lista recomendada para rotação)
   - `Authentication:AtendimentoApiKey` (compatibilidade com chave única)
   - `Authentication:AllowAnonymousInDevelopment`
   - `RateLimiting:Atendimento:PermitLimit`
   - `RateLimiting:Atendimento:WindowSeconds`
   - `RateLimiting:Atendimento:QueueLimit`

## Execução local

```bash
dotnet run --project Secretaria.api/Secretaria.api.csproj
```

## Endpoints

- `POST /api/atendimento`
- `GET /health`

### Autenticação do endpoint de atendimento

`POST /api/atendimento` exige o header `X-Atendimento-Api-Key`.

- Em ambientes não Development, pelo menos uma chave deve estar configurada.
- Em Development, o bypass pode ser habilitado com `Authentication:AllowAnonymousInDevelopment=true`.

### Rate limiting

O endpoint `POST /api/atendimento` possui limitação de taxa configurável e retorna `429 Too Many Requests` quando excedido.

### Telemetria de segurança

A API registra eventos de segurança em logs estruturados com `traceId` para:

- API key inválida no endpoint de atendimento;
- rejeições por rate limiting;
- configuração inválida de autenticação.

Também publica métricas em `/metrics` (formato Prometheus):

- `atendimento_auth_success_total`
- `atendimento_auth_failures_total`
- `atendimento_auth_config_errors_total`
- `atendimento_rate_limit_rejections_total`

Exemplos de consultas:

- `sum(rate(atendimento_auth_failures_total[5m]))`
- `sum(rate(atendimento_rate_limit_rejections_total[5m]))`
- `sum(rate(atendimento_auth_success_total[5m]))`

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
- autenticação por API key no endpoint de atendimento;
- rate limiting no endpoint de atendimento;
- telemetria de segurança com logs estruturados e métricas em `/metrics`;
- pipeline de CI com restore, build e test em [ci.yml](./.github/workflows/ci.yml).
