using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Secretaria.api.Model.Settings;
using Secretaria.api.Services;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "DefaultCorsPolicy";
const string AtendimentoRateLimiterPolicy = "AtendimentoRequests";
const string AtendimentoApiKeyHeaderName = "X-Atendimento-Api-Key";
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var atendimentoPermitLimit = ReadPositiveInt(builder.Configuration, "RateLimiting:Atendimento:PermitLimit");
var atendimentoWindowSeconds = ReadPositiveInt(builder.Configuration, "RateLimiting:Atendimento:WindowSeconds");
var atendimentoQueueLimit = ReadNonNegativeInt(builder.Configuration, "RateLimiting:Atendimento:QueueLimit");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.OnRejected = async (context, cancellationToken) =>
	{
		if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
		{
			context.HttpContext.Response.Headers.RetryAfter =
				Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
		}

		var problem = new ProblemDetails
		{
			Status = StatusCodes.Status429TooManyRequests,
			Title = "Limite de requisições excedido",
			Detail = "Aguarde alguns instantes e tente novamente."
		};

		problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
		await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
	};

	options.AddFixedWindowLimiter(AtendimentoRateLimiterPolicy, limiterOptions =>
	{
		limiterOptions.PermitLimit = atendimentoPermitLimit;
		limiterOptions.Window = TimeSpan.FromSeconds(atendimentoWindowSeconds);
		limiterOptions.QueueLimit = atendimentoQueueLimit;
		limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
	});
});
builder.Services.AddProblemDetails(options =>
{
	options.CustomizeProblemDetails = context =>
	{
		context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
	};
});

builder.Services.AddCors(options =>
{
	options.AddPolicy(CorsPolicyName, policy =>
	{
		if (builder.Environment.IsDevelopment())
		{
			policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
			return;
		}

		if (allowedCorsOrigins.Length == 0)
			throw new InvalidOperationException("Configure Cors:AllowedOrigins para ambientes não Development.");

		policy.WithOrigins(allowedCorsOrigins).AllowAnyHeader().AllowAnyMethod();
	});
});

builder.Services
	.AddOptions<AgentSettings>()
	.Bind(builder.Configuration.GetSection("Agent"))
	.ValidateDataAnnotations()
	.ValidateOnStart();

builder.Services
	.AddOptions<AtendimentoResilienceSettings>()
	.Bind(builder.Configuration.GetSection("AtendimentoResilience"))
	.ValidateDataAnnotations()
	.ValidateOnStart();

builder.Services.AddScoped<IAtendimentoClient, AzureAtendimentoClient>();
builder.Services.AddScoped<AtendimentoServices>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "Secretaria API v1");
		c.RoutePrefix = string.Empty;
	});
}
else
{
	app.UseHsts();
	app.UseHttpsRedirection();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
	exceptionHandlerApp.Run(async context =>
	{
		var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
		var problem = new ProblemDetails
		{
			Status = StatusCodes.Status500InternalServerError,
			Title = "Erro inesperado",
			Detail = app.Environment.IsDevelopment()
				? exception?.Message
				: "Consulte o traceId para suporte."
		};

		problem.Extensions["traceId"] = context.TraceIdentifier;

		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		await context.Response.WriteAsJsonAsync(problem);
	});
});

app.UseCors(CorsPolicyName);
app.UseRateLimiter();
app.Use(async (context, next) =>
{
	if (!context.Request.Path.StartsWithSegments("/api/atendimento", StringComparison.OrdinalIgnoreCase))
	{
		await next();
		return;
	}

	var allowAnonymousInDevelopment = app.Configuration.GetValue("Authentication:AllowAnonymousInDevelopment", true);
	var configuredApiKeys = ReadConfiguredApiKeys(app.Configuration);
	var bypassAuthentication = app.Environment.IsDevelopment() && allowAnonymousInDevelopment;

	if (configuredApiKeys.Count == 0)
	{
		if (bypassAuthentication)
		{
			await next();
			return;
		}

		var configurationProblem = new ProblemDetails
		{
			Status = StatusCodes.Status500InternalServerError,
			Title = "Configuração inválida",
			Detail = "Defina Authentication:AtendimentoApiKeys (ou Authentication:AtendimentoApiKey)."
		};

		configurationProblem.Extensions["traceId"] = context.TraceIdentifier;
		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		await context.Response.WriteAsJsonAsync(configurationProblem);
		return;
	}

	var providedApiKey = context.Request.Headers[AtendimentoApiKeyHeaderName].ToString();
	if (!IsValidApiKey(configuredApiKeys, providedApiKey))
	{
		var problem = new ProblemDetails
		{
			Status = StatusCodes.Status401Unauthorized,
			Title = "Não autorizado",
			Detail = $"Informe o header {AtendimentoApiKeyHeaderName} com uma API key válida."
		};

		problem.Extensions["traceId"] = context.TraceIdentifier;
		context.Response.StatusCode = StatusCodes.Status401Unauthorized;
		await context.Response.WriteAsJsonAsync(problem);
		return;
	}

	await next();
});
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting(AtendimentoRateLimiterPolicy);

app.Run();

static int ReadPositiveInt(IConfiguration configuration, string key)
{
	var value = configuration.GetValue<int?>(key)
		?? throw new InvalidOperationException($"Configuração obrigatória ausente: {key}");

	if (value <= 0)
		throw new InvalidOperationException($"Configuração inválida para {key}. Informe um número maior que zero.");

	return value;
}

static int ReadNonNegativeInt(IConfiguration configuration, string key)
{
	var value = configuration.GetValue<int?>(key)
		?? throw new InvalidOperationException($"Configuração obrigatória ausente: {key}");

	if (value < 0)
		throw new InvalidOperationException($"Configuração inválida para {key}. Informe zero ou um número positivo.");

	return value;
}

static IReadOnlyList<string> ReadConfiguredApiKeys(IConfiguration configuration)
{
	var configuredKeys = configuration.GetSection("Authentication:AtendimentoApiKeys").Get<string[]>() ?? [];
	var keys = new List<string>();

	foreach (var key in configuredKeys)
	{
		if (!string.IsNullOrWhiteSpace(key))
			keys.Add(key.Trim());
	}

	var fallbackKey = configuration["Authentication:AtendimentoApiKey"];
	if (!string.IsNullOrWhiteSpace(fallbackKey))
		keys.Add(fallbackKey.Trim());

	return keys.Distinct(StringComparer.Ordinal).ToArray();
}

static bool IsValidApiKey(IReadOnlyList<string> expectedApiKeys, string providedApiKey)
{
	if (expectedApiKeys.Count == 0 || string.IsNullOrEmpty(providedApiKey))
		return false;

	var provided = Encoding.UTF8.GetBytes(providedApiKey);

	foreach (var expectedApiKey in expectedApiKeys)
	{
		var expected = Encoding.UTF8.GetBytes(expectedApiKey);
		if (expected.Length == provided.Length && CryptographicOperations.FixedTimeEquals(expected, provided))
			return true;
	}

	return false;
}

public partial class Program;
