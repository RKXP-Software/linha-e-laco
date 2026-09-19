using System.Text.Json;
using Npgsql;

namespace LinhaELaco.Api;

public sealed class DatabaseStore
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public DatabaseStore(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<Client>> GetClients()
    {
        const string sql = "select id, name, cpf, email, phone, address::text, notes, created_at from clients order by name";
        await using var cmd = _dataSource.CreateCommand(sql);
        await using var reader = await cmd.ExecuteReaderAsync();
        var result = new List<Client>();
        while (await reader.ReadAsync()) result.Add(ReadClient(reader));
        return result;
    }

    public async Task<Client?> GetClient(Guid id)
    {
        const string sql = "select id, name, cpf, email, phone, address::text, notes, created_at from clients where id = @id";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadClient(reader) : null;
    }

    public async Task<Client> CreateClient(CreateClientRequest request)
    {
        const string sql = "insert into clients (name, cpf, email, phone, address, notes) values (@name, @cpf, @email, @phone, cast(@address as jsonb), @notes) returning id";
        await using var cmd = _dataSource.CreateCommand(sql);
        AddClientParameters(cmd, request);
        var id = (Guid)(await cmd.ExecuteScalarAsync())!;
        return (await GetClient(id))!;
    }

    public async Task<Client?> UpdateClient(Guid id, CreateClientRequest request)
    {
        const string sql = "update clients set name=@name, cpf=@cpf, email=@email, phone=@phone, address=cast(@address as jsonb), notes=@notes where id=@id";
        await using var cmd = _dataSource.CreateCommand(sql);
        AddClientParameters(cmd, request);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() == 0 ? null : await GetClient(id);
    }

    public async Task<bool> DeleteClient(Guid id)
    {
        await using var cmd = _dataSource.CreateCommand("delete from clients where id=@id");
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<IReadOnlyList<CatalogItem>> GetCatalog(bool includeInactive = true)
    {
        var sql = "select id, kind::text, name, description, base_price, estimated_days, service_detail::text, garment_detail::text, active from catalog_items" + (includeInactive ? "" : " where active") + " order by active desc, name";
        await using var cmd = _dataSource.CreateCommand(sql);
        await using var reader = await cmd.ExecuteReaderAsync();
        var result = new List<CatalogItem>();
        while (await reader.ReadAsync()) result.Add(ReadCatalog(reader));
        return result;
    }

    public async Task<CatalogItem?> GetCatalogItem(Guid id)
    {
        const string sql = "select id, kind::text, name, description, base_price, estimated_days, service_detail::text, garment_detail::text, active from catalog_items where id=@id";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadCatalog(reader) : null;
    }

    public async Task<CatalogItem> CreateCatalog(CreateCatalogItemRequest request)
    {
        const string sql = "insert into catalog_items (kind,name,description,base_price,estimated_days,service_detail,garment_detail) values (cast(@kind as catalog_kind),@name,@description,@price,@days,cast(@service as jsonb),cast(@garment as jsonb)) returning id";
        await using var cmd = _dataSource.CreateCommand(sql);
        AddCatalogParameters(cmd, request);
        var id = (Guid)(await cmd.ExecuteScalarAsync())!;
        return (await GetCatalogItem(id))!;
    }

    public async Task<CatalogItem?> UpdateCatalog(Guid id, CreateCatalogItemRequest request)
    {
        const string sql = "update catalog_items set kind=cast(@kind as catalog_kind), name=@name, description=@description, base_price=@price, estimated_days=@days, service_detail=cast(@service as jsonb), garment_detail=cast(@garment as jsonb) where id=@id";
        await using var cmd = _dataSource.CreateCommand(sql);
        AddCatalogParameters(cmd, request);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() == 0 ? null : await GetCatalogItem(id);
    }

    public async Task<CatalogItem?> SetCatalogActive(Guid id, bool active)
    {
        await using var cmd = _dataSource.CreateCommand("update catalog_items set active=@active where id=@id");
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("active", active);
        return await cmd.ExecuteNonQueryAsync() == 0 ? null : await GetCatalogItem(id);
    }

    public async Task<IReadOnlyList<Order>> GetOrders()
    {
        const string sql = "select id, number, client_id, status::text, created_at::date, promised_for, notes from orders order by created_at desc";
        await using var cmd = _dataSource.CreateCommand(sql);
        await using var reader = await cmd.ExecuteReaderAsync();
        var heads = new List<(Guid Id, string Number, Guid ClientId, OrderStatus Status, DateOnly CreatedOn, DateOnly? PromisedFor, string? Notes)>();
        while (await reader.ReadAsync()) heads.Add((reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), ParseStatus(reader.GetString(3)), reader.GetFieldValue<DateOnly>(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
        var result = new List<Order>();
        foreach (var head in heads) result.Add(await GetOrder(head));
        return result;
    }

    public async Task<Order?> GetOrder(Guid id)
    {
        const string sql = "select id, number, client_id, status::text, created_at::date, promised_for, notes from orders where id=@id";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return await GetOrder((reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), ParseStatus(reader.GetString(3)), reader.GetFieldValue<DateOnly>(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
    }

    public async Task<Order> CreateOrder(CreateOrderRequest request)
    {
        var catalog = (await GetCatalog(false)).ToDictionary(item => item.Id);
        if (request.Items.Count == 0 || request.Items.Any(item => !catalog.ContainsKey(item.CatalogItemId))) throw new ArgumentException("Selecione ao menos um item ativo do catálogo.");
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var id = Guid.NewGuid();
        var number = $"LL-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
        await using (var order = new NpgsqlCommand("insert into orders (id,number,client_id,status,promised_for,notes) values (@id,@number,@client,cast('quoted' as order_status),@promised,@notes)", connection, transaction))
        {
            order.Parameters.AddWithValue("id", id); order.Parameters.AddWithValue("number", number); order.Parameters.AddWithValue("client", request.ClientId); order.Parameters.AddWithValue("promised", (object?)request.PromisedFor ?? DBNull.Value); order.Parameters.AddWithValue("notes", (object?)request.Notes ?? DBNull.Value);
            await order.ExecuteNonQueryAsync();
        }
        foreach (var item in request.Items)
        {
            var source = catalog[item.CatalogItemId];
            await using var line = new NpgsqlCommand("insert into order_items (order_id,catalog_item_id,name_snapshot,kind,unit_price,quantity,notes) values (@order,@catalog,@name,cast(@kind as catalog_kind),@price,@quantity,@notes)", connection, transaction);
            line.Parameters.AddWithValue("order", id); line.Parameters.AddWithValue("catalog", source.Id); line.Parameters.AddWithValue("name", source.Name); line.Parameters.AddWithValue("kind", ToDb(source.Kind)); line.Parameters.AddWithValue("price", item.UnitPrice ?? source.BasePrice); line.Parameters.AddWithValue("quantity", item.Quantity); line.Parameters.AddWithValue("notes", (object?)item.Notes ?? DBNull.Value); await line.ExecuteNonQueryAsync();
        }
        for (var index = 0; index < request.Installments.Count; index++)
        {
            var installment = request.Installments[index];
            await using var payment = new NpgsqlCommand("insert into payment_installments (order_id,installment_number,amount,due_date,status) values (@order,@number,@amount,@due,cast(@status as installment_status))", connection, transaction);
            payment.Parameters.AddWithValue("order", id); payment.Parameters.AddWithValue("number", index + 1); payment.Parameters.AddWithValue("amount", installment.Amount); payment.Parameters.AddWithValue("due", installment.DueDate); payment.Parameters.AddWithValue("status", installment.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) ? "overdue" : "open"); await payment.ExecuteNonQueryAsync();
        }
        await using (var history = new NpgsqlCommand("insert into order_status_history (order_id,status) values (@order,cast('quoted' as order_status))", connection, transaction)) { history.Parameters.AddWithValue("order", id); await history.ExecuteNonQueryAsync(); }
        await transaction.CommitAsync();
        return (await GetOrder(id))!;
    }

    public async Task<Order?> ChangeOrderStatus(Guid id, OrderStatus status)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var cmd = new NpgsqlCommand("update orders set status=cast(@status as order_status) where id=@id", connection, transaction)) { cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("status", ToDb(status)); if (await cmd.ExecuteNonQueryAsync() == 0) return null; }
        await using (var history = new NpgsqlCommand("insert into order_status_history (order_id,status) values (@id,cast(@status as order_status))", connection, transaction)) { history.Parameters.AddWithValue("id", id); history.Parameters.AddWithValue("status", ToDb(status)); await history.ExecuteNonQueryAsync(); }
        await transaction.CommitAsync();
        return await GetOrder(id);
    }

    public async Task<bool> MarkInstallmentPaid(Guid id)
    {
        await using var cmd = _dataSource.CreateCommand("update payment_installments set status=cast('paid' as installment_status), paid_at=current_date where id=@id");
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<IReadOnlyList<Note>> GetNotes()
    {
        await using var cmd = _dataSource.CreateCommand("select id,text,is_done,created_at from notes order by is_done,created_at desc");
        await using var reader = await cmd.ExecuteReaderAsync(); var notes = new List<Note>();
        while (await reader.ReadAsync()) notes.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2), reader.GetFieldValue<DateTimeOffset>(3)));
        return notes;
    }

    public async Task<Note> CreateNote(CreateNoteRequest request)
    {
        await using var cmd = _dataSource.CreateCommand("insert into notes (text) values (@text) returning id,text,is_done,created_at"); cmd.Parameters.AddWithValue("text", request.Text.Trim());
        await using var reader = await cmd.ExecuteReaderAsync(); await reader.ReadAsync(); return new(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2), reader.GetFieldValue<DateTimeOffset>(3));
    }

    public async Task<Note?> SetNoteStatus(Guid id, bool done)
    {
        await using var cmd = _dataSource.CreateCommand("update notes set is_done=@done where id=@id returning id,text,is_done,created_at"); cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("done", done);
        await using var reader = await cmd.ExecuteReaderAsync(); return await reader.ReadAsync() ? new(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2), reader.GetFieldValue<DateTimeOffset>(3)) : null;
    }

    public async Task<bool> DeleteNote(Guid id) { await using var cmd = _dataSource.CreateCommand("delete from notes where id=@id"); cmd.Parameters.AddWithValue("id", id); return await cmd.ExecuteNonQueryAsync() > 0; }

    public async Task<DashboardResponse> GetDashboard(DateOnly from, DateOnly to)
    {
        var orders = (await GetOrders()).Where(order => order.CreatedOn >= from && order.CreatedOn <= to && order.Status != OrderStatus.Cancelled).ToList();
        var payments = orders.SelectMany(order => order.Installments).ToList();
        var received = payments.Where(item => item.Status == InstallmentStatus.Paid).Sum(item => item.Amount);
        var expected = orders.Sum(item => item.Total);
        var overdue = payments.Where(item => item.Status == InstallmentStatus.Overdue).Sum(item => item.Amount);
        var statuses = orders.GroupBy(item => item.Status.ToString()).ToDictionary(group => group.Key, group => group.Count());
        var sellers = orders.SelectMany(order => order.Items).GroupBy(item => item.NameSnapshot).OrderByDescending(group => group.Sum(item => item.Quantity)).Take(5).Select(group => new RankedCatalogItem(group.Key, group.Sum(item => item.Quantity))).ToList();
        var all = await GetOrders(); var quoted = all.Count(order => order.Status != OrderStatus.Cancelled); var approved = all.Count(order => order.Status is OrderStatus.Approved or OrderStatus.InProduction or OrderStatus.Ready or OrderStatus.Delivered);
        return new(received, expected, overdue, orders.Count, orders.Count == 0 ? 0 : orders.Average(order => order.Total), orders.Select(order => order.ClientId).Distinct().Count(), statuses, sellers, quoted == 0 ? 0 : Math.Round((decimal)approved / quoted * 100, 1));
    }

    private async Task<Order> GetOrder((Guid Id, string Number, Guid ClientId, OrderStatus Status, DateOnly CreatedOn, DateOnly? PromisedFor, string? Notes) head)
    {
        var items = new List<OrderItem>(); await using (var cmd = _dataSource.CreateCommand("select catalog_item_id,name_snapshot,kind::text,unit_price,quantity,notes from order_items where order_id=@id order by id")) { cmd.Parameters.AddWithValue("id", head.Id); await using var reader = await cmd.ExecuteReaderAsync(); while (await reader.ReadAsync()) items.Add(new(reader.IsDBNull(0) ? Guid.Empty : reader.GetGuid(0), reader.GetString(1), ParseKind(reader.GetString(2)), reader.GetDecimal(3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetString(5))); }
        var payments = new List<PaymentInstallment>(); await using (var cmd = _dataSource.CreateCommand("select id,installment_number,amount,due_date,paid_at,status::text from payment_installments where order_id=@id order by installment_number")) { cmd.Parameters.AddWithValue("id", head.Id); await using var reader = await cmd.ExecuteReaderAsync(); while (await reader.ReadAsync()) payments.Add(new(reader.GetGuid(0), reader.GetInt32(1), reader.GetDecimal(2), reader.GetFieldValue<DateOnly>(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateOnly>(4), ParseInstallment(reader.GetString(5)))); }
        return new(head.Id, head.Number, head.ClientId, head.Status, head.CreatedOn, head.PromisedFor, items, payments, head.Notes);
    }

    private Client ReadClient(NpgsqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : JsonSerializer.Deserialize<Address>(reader.GetString(5), _json), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetFieldValue<DateTimeOffset>(7));
    private CatalogItem ReadCatalog(NpgsqlDataReader reader) => new(reader.GetGuid(0), ParseKind(reader.GetString(1)), reader.GetString(2), reader.GetString(3), reader.GetDecimal(4), reader.GetInt32(5), reader.IsDBNull(6) ? null : JsonSerializer.Deserialize<ServiceDetail>(reader.GetString(6), _json), reader.IsDBNull(7) ? null : JsonSerializer.Deserialize<GarmentDetail>(reader.GetString(7), _json), reader.GetBoolean(8));
    private void AddClientParameters(NpgsqlCommand cmd, CreateClientRequest request) { cmd.Parameters.AddWithValue("name", request.Name.Trim()); cmd.Parameters.AddWithValue("cpf", (object?)request.Cpf ?? DBNull.Value); cmd.Parameters.AddWithValue("email", (object?)request.Email ?? DBNull.Value); cmd.Parameters.AddWithValue("phone", (object?)request.Phone ?? DBNull.Value); cmd.Parameters.AddWithValue("address", request.Address is null ? "null" : JsonSerializer.Serialize(request.Address, _json)); cmd.Parameters.AddWithValue("notes", (object?)request.Notes ?? DBNull.Value); }
    private void AddCatalogParameters(NpgsqlCommand cmd, CreateCatalogItemRequest request) { cmd.Parameters.AddWithValue("kind", ToDb(request.Kind)); cmd.Parameters.AddWithValue("name", request.Name.Trim()); cmd.Parameters.AddWithValue("description", request.Description ?? ""); cmd.Parameters.AddWithValue("price", request.BasePrice); cmd.Parameters.AddWithValue("days", request.EstimatedDays); cmd.Parameters.AddWithValue("service", request.Kind == CatalogKind.Service ? JsonSerializer.Serialize(request.Service, _json) : "null"); cmd.Parameters.AddWithValue("garment", request.Kind == CatalogKind.Garment ? JsonSerializer.Serialize(request.Garment, _json) : "null"); }
    private static string ToDb(CatalogKind value) => value == CatalogKind.Service ? "service" : "garment";
    private static string ToDb(OrderStatus value) => value switch { OrderStatus.Quoted => "quoted", OrderStatus.Approved => "approved", OrderStatus.InProduction => "in_production", OrderStatus.Ready => "ready", OrderStatus.Delivered => "delivered", _ => "cancelled" };
    private static CatalogKind ParseKind(string value) => value == "service" ? CatalogKind.Service : CatalogKind.Garment;
    private static OrderStatus ParseStatus(string value) => value switch { "quoted" => OrderStatus.Quoted, "approved" => OrderStatus.Approved, "in_production" => OrderStatus.InProduction, "ready" => OrderStatus.Ready, "delivered" => OrderStatus.Delivered, _ => OrderStatus.Cancelled };
    private static InstallmentStatus ParseInstallment(string value) => value switch { "paid" => InstallmentStatus.Paid, "overdue" => InstallmentStatus.Overdue, _ => InstallmentStatus.Open };
}