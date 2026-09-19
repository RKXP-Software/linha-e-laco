using LinhaELaco.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:5173", "http://localhost:5174", "http://127.0.0.1:5173", "http://127.0.0.1:5174")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddSingleton<DemoStore>();

var app = builder.Build();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok", application = "Linha & Laço" }));

var api = app.MapGroup("/api");

api.MapGet("/dashboard", (DemoStore store, DateOnly? from, DateOnly? to) =>
{
    var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
    var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
    return Results.Ok(store.GetDashboard(start, end));
});

api.MapGet("/notes", (DemoStore store) => Results.Ok(store.Notes.OrderBy(note => note.IsDone).ThenByDescending(note => note.CreatedAt)));
api.MapPost("/notes", (CreateNoteRequest request, DemoStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Text)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["text"] = ["Escreva uma anotação."] });
    var note = store.AddNote(request);
    return Results.Created($"/api/notes/{note.Id}", note);
});
api.MapPatch("/notes/{id:guid}", (Guid id, ChangeNoteStatusRequest request, DemoStore store) =>
{
    var note = store.ChangeNoteStatus(id, request.IsDone);
    return note is null ? Results.NotFound() : Results.Ok(note);
});
api.MapDelete("/notes/{id:guid}", (Guid id, DemoStore store) => store.DeleteNote(id) ? Results.NoContent() : Results.NotFound());

api.MapGet("/clients", (DemoStore store) => Results.Ok(store.Clients));
api.MapPost("/clients", (CreateClientRequest request, DemoStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório."] });
    var client = store.AddClient(request);
    return Results.Created($"/api/clients/{client.Id}", client);
});

api.MapGet("/catalog", (DemoStore store) => Results.Ok(store.Catalog));
api.MapPost("/catalog", (CreateCatalogItemRequest request, DemoStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.BasePrice < 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["catalog"] = ["Nome e preço-base válido são obrigatórios."] });
    var item = store.AddCatalogItem(request);
    return Results.Created($"/api/catalog/{item.Id}", item);
});

api.MapGet("/orders", (DemoStore store) => Results.Ok(store.Orders));
api.MapPost("/orders", (CreateOrderRequest request, DemoStore store) =>
{
    if (!store.Clients.Any(client => client.Id == request.ClientId) || request.Items.Count == 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = ["Cliente existente e ao menos um item são obrigatórios."] });
    var order = store.AddOrder(request);
    return Results.Created($"/api/orders/{order.Id}", order);
});

api.MapPatch("/orders/{id:guid}/status", (Guid id, ChangeOrderStatusRequest request, DemoStore store) =>
{
    var updated = store.ChangeOrderStatus(id, request.Status);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

api.MapGet("/orders/{id:guid}/receipt", (Guid id, DemoStore store) =>
{
    var order = store.Orders.SingleOrDefault(item => item.Id == id);
    if (order is null) return Results.NotFound();
    var client = store.Clients.Single(item => item.Id == order.ClientId);
    var pdf = ReceiptPdf.Create(order, client);
    return Results.File(pdf, "application/pdf", $"recibo-{order.Number}.pdf");
});

app.Run();
