using Microsoft.OpenApi;
using Secretaria.api.Model.Settings;
using Secretaria.api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS to allow browser clients to call the API (ajuste a origem conforme necessário)
const string CorsPolicyName = "DefaultCorsPolicy";
builder.Services.AddCors(options =>
{
	options.AddPolicy(CorsPolicyName, policy =>
	{
		policy
			.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

// Bind Agent section from configuration to strongly-typed settings
builder.Services.Configure<AgentSettings>(builder.Configuration.GetSection("Agent"));
// Register domain services
builder.Services.AddScoped<AtendimentoServices>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "Secretaria API v1");
		c.RoutePrefix = string.Empty; // abre Swagger UI na raiz /
	});
}

// Não redireciona mais HTTP para HTTPS; API pode ser acessada via HTTP simples

// Enable CORS middleware
app.UseCors(CorsPolicyName);

app.UseAuthorization();

app.MapControllers();

app.Run();
