using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Secretaria.api.Model.Settings;
using Secretaria.api.Services;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "DefaultCorsPolicy";
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
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
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
