using LinhaELaco.Api;
using Npgsql;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var localOrigins = new[] { "http://localhost:5173", "http://localhost:5174", "http://127.0.0.1:5173", "http://127.0.0.1:5174" };
var configuredOrigins = builder.Configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
var allowedOrigins = localOrigins.Concat(configuredOrigins).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("Supabase");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("ConnectionStrings__Supabase deve ser configurada.");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<DatabaseStore>();

var app = builder.Build();
app.UseCors();
app.MapGet("/health", async (NpgsqlDataSource dataSource) => { await using var cmd = dataSource.CreateCommand("select 1"); await cmd.ExecuteScalarAsync(); return Results.Ok(new { status = "ok", application = "Linha & Laço", database = "connected" }); });

var api = app.MapGroup("/api");
api.MapGet("/dashboard", async (DatabaseStore store, DateOnly? from, DateOnly? to) => Results.Ok(await store.GetDashboard(from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), to ?? DateOnly.FromDateTime(DateTime.UtcNow))));
api.MapGet("/clients", async (DatabaseStore store) => Results.Ok(await store.GetClients()));
api.MapGet("/clients/{id:guid}", async (Guid id, DatabaseStore store) => await store.GetClient(id) is { } client ? Results.Ok(client) : Results.NotFound());
api.MapPost("/clients", async (CreateClientRequest request, DatabaseStore store) => string.IsNullOrWhiteSpace(request.Name) ? Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório."] }) : Results.Created("/api/clients", await store.CreateClient(request)));
api.MapPut("/clients/{id:guid}", async (Guid id, CreateClientRequest request, DatabaseStore store) => string.IsNullOrWhiteSpace(request.Name) ? Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório."] }) : await store.UpdateClient(id, request) is { } client ? Results.Ok(client) : Results.NotFound());
api.MapDelete("/clients/{id:guid}", async (Guid id, DatabaseStore store) => await store.DeleteClient(id) ? Results.NoContent() : Results.NotFound());

api.MapGet("/catalog", async (DatabaseStore store, bool? activeOnly) => Results.Ok(await store.GetCatalog(activeOnly == true ? false : true)));
api.MapPost("/catalog", async (CreateCatalogItemRequest request, DatabaseStore store) => DomainRules.IsValidCatalog(request) ? Results.Created("/api/catalog", await store.CreateCatalog(request)) : Results.ValidationProblem(new Dictionary<string, string[]> { ["catalog"] = ["Preencha nome, preço e os detalhes do tipo escolhido."] }));
api.MapPut("/catalog/{id:guid}", async (Guid id, CreateCatalogItemRequest request, DatabaseStore store) => DomainRules.IsValidCatalog(request) ? await store.UpdateCatalog(id, request) is { } item ? Results.Ok(item) : Results.NotFound() : Results.ValidationProblem(new Dictionary<string, string[]> { ["catalog"] = ["Preencha nome, preço e os detalhes do tipo escolhido."] }));
api.MapPatch("/catalog/{id:guid}/active", async (Guid id, ChangeActiveRequest request, DatabaseStore store) => await store.SetCatalogActive(id, request.Active) is { } item ? Results.Ok(item) : Results.NotFound());

api.MapGet("/orders", async (DatabaseStore store) => Results.Ok(await store.GetOrders()));
api.MapGet("/orders/{id:guid}", async (Guid id, DatabaseStore store) => await store.GetOrder(id) is { } order ? Results.Ok(order) : Results.NotFound());
api.MapPost("/orders", async (CreateOrderRequest request, DatabaseStore store) => { if (request.Items.Count == 0) return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Inclua ao menos um item."] }); try { var order = await store.CreateOrder(request); return Results.Created($"/api/orders/{order.Id}", order); } catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [exception.Message] }); } });
api.MapPatch("/orders/{id:guid}/status", async (Guid id, ChangeOrderStatusRequest request, DatabaseStore store) => await store.ChangeOrderStatus(id, request.Status) is { } order ? Results.Ok(order) : Results.NotFound());
api.MapPost("/installments/{id:guid}/pay", async (Guid id, DatabaseStore store) => await store.MarkInstallmentPaid(id) ? Results.NoContent() : Results.NotFound());
api.MapGet("/orders/{id:guid}/receipt", async (Guid id, DatabaseStore store) => { var order = await store.GetOrder(id); if (order is null) return Results.NotFound(); var client = await store.GetClient(order.ClientId); return client is null ? Results.NotFound() : Results.File(ReceiptPdf.Create(order, client), "application/pdf", $"recibo-{order.Number}.pdf"); });

api.MapGet("/notes", async (DatabaseStore store) => Results.Ok(await store.GetNotes()));
api.MapPost("/notes", async (CreateNoteRequest request, DatabaseStore store) => string.IsNullOrWhiteSpace(request.Text) ? Results.ValidationProblem(new Dictionary<string, string[]> { ["text"] = ["Escreva uma anotação."] }) : Results.Created("/api/notes", await store.CreateNote(request)));
api.MapPatch("/notes/{id:guid}", async (Guid id, ChangeNoteStatusRequest request, DatabaseStore store) => await store.SetNoteStatus(id, request.IsDone) is { } note ? Results.Ok(note) : Results.NotFound());
api.MapDelete("/notes/{id:guid}", async (Guid id, DatabaseStore store) => await store.DeleteNote(id) ? Results.NoContent() : Results.NotFound());

app.Run();

public record ChangeActiveRequest(bool Active);