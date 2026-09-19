using System.Globalization;
using System.Text;

namespace LinhaELaco.Api;

public sealed class DemoStore
{
    public List<Client> Clients { get; } = [];
    public List<CatalogItem> Catalog { get; } = [];
    public List<Order> Orders { get; } = [];
    public List<Note> Notes { get; } = [];

    public DemoStore()
    {
        var ana = AddClient(new("Ana Souza", null, "ana@exemplo.com", "(11) 99999-1000", new Address("Rua das Flores", "100", null, "Centro", "São Paulo", "SP", "01000-000"), "Prefere contato por telefone."));
        var barra = AddCatalogItem(new(CatalogKind.Service, "Ajuste de barra", "Ajuste de comprimento para calças e saias.", 45m, 3, new ServiceDetail("peça", "Trazer a peça para prova."), null));
        var vestido = AddCatalogItem(new(CatalogKind.Garment, "Vestido sob medida", "Vestido para eventos, confeccionado sob medida.", 480m, 20, null, new GarmentDetail("Vestido", "Busto, cintura, quadril e comprimento", "Modelagem; corte; primeira prova; acabamento", "Tecido principal, forro e zíper")));
        AddOrder(new(ana.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(12)), [new CreateOrderItemRequest(vestido.Id, null, 1, "Azul-marinho")], [new CreateInstallmentRequest(240m, DateOnly.FromDateTime(DateTime.Today)), new CreateInstallmentRequest(240m, DateOnly.FromDateTime(DateTime.Today.AddDays(12)))], "Aguardando primeira prova."), OrderStatus.InProduction, true);
        AddOrder(new(ana.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(3)), [new CreateOrderItemRequest(barra.Id, null, 1, null)], [new CreateInstallmentRequest(45m, DateOnly.FromDateTime(DateTime.Today.AddDays(3)))], null), OrderStatus.Quoted, false);
        Notes.Add(new Note(Guid.NewGuid(), "Confirmar medida da cintura com Ana.", false, DateTimeOffset.UtcNow));
        Notes.Add(new Note(Guid.NewGuid(), "Retornar contato de nova cliente sobre ajuste de vestido.", false, DateTimeOffset.UtcNow));
    }

    public Client AddClient(CreateClientRequest request)
    {
        var client = new Client(Guid.NewGuid(), request.Name.Trim(), request.Cpf, request.Email, request.Phone, request.Address, request.Notes, DateTimeOffset.UtcNow);
        Clients.Add(client);
        return client;
    }

    public CatalogItem AddCatalogItem(CreateCatalogItemRequest request)
    {
        var item = new CatalogItem(Guid.NewGuid(), request.Kind, request.Name.Trim(), request.Description, request.BasePrice, request.EstimatedDays, request.Service, request.Garment, true);
        Catalog.Add(item);
        return item;
    }

    public Order AddOrder(CreateOrderRequest request) => AddOrder(request, OrderStatus.Quoted, false);

    private Order AddOrder(CreateOrderRequest request, OrderStatus status, bool firstInstallmentPaid)
    {
        var items = request.Items.Select(requestItem =>
        {
            var catalogItem = Catalog.Single(item => item.Id == requestItem.CatalogItemId);
            return new OrderItem(catalogItem.Id, catalogItem.Name, catalogItem.Kind, requestItem.UnitPrice ?? catalogItem.BasePrice, Math.Max(1, requestItem.Quantity), requestItem.Notes);
        }).ToList();
        var installments = request.Installments.Select((installment, index) => new PaymentInstallment(Guid.NewGuid(), index + 1, installment.Amount, installment.DueDate, firstInstallmentPaid && index == 0 ? DateOnly.FromDateTime(DateTime.Today) : null, firstInstallmentPaid && index == 0 ? InstallmentStatus.Paid : installment.DueDate < DateOnly.FromDateTime(DateTime.Today) ? InstallmentStatus.Overdue : InstallmentStatus.Open)).ToList();
        var order = new Order(Guid.NewGuid(), $"LL-{Orders.Count + 1:0000}", request.ClientId, status, DateOnly.FromDateTime(DateTime.Today), request.PromisedFor, items, installments, request.Notes);
        Orders.Add(order);
        return order;
    }

    public Order? ChangeOrderStatus(Guid id, OrderStatus status)
    {
        var index = Orders.FindIndex(item => item.Id == id);
        if (index < 0) return null;
        var changed = Orders[index] with { Status = status };
        Orders[index] = changed;
        return changed;
    }

    public Note AddNote(CreateNoteRequest request)
    {
        var note = new Note(Guid.NewGuid(), request.Text.Trim(), false, DateTimeOffset.UtcNow);
        Notes.Add(note);
        return note;
    }

    public Note? ChangeNoteStatus(Guid id, bool isDone)
    {
        var index = Notes.FindIndex(note => note.Id == id);
        if (index < 0) return null;
        var note = Notes[index] with { IsDone = isDone };
        Notes[index] = note;
        return note;
    }

    public bool DeleteNote(Guid id) => Notes.RemoveAll(note => note.Id == id) > 0;

    public DashboardResponse GetDashboard(DateOnly from, DateOnly to)
    {
        var periodOrders = Orders.Where(order => order.CreatedOn >= from && order.CreatedOn <= to && order.Status != OrderStatus.Cancelled).ToList();
        var installments = periodOrders.SelectMany(order => order.Installments).ToList();
        var received = installments.Where(item => item.Status == InstallmentStatus.Paid).Sum(item => item.Amount);
        var expected = periodOrders.Sum(item => item.Total);
        var overdue = installments.Where(item => item.Status == InstallmentStatus.Overdue).Sum(item => item.Amount);
        var statusCounts = periodOrders.GroupBy(item => item.Status.ToString()).ToDictionary(group => group.Key, group => group.Count());
        var sellers = periodOrders.SelectMany(order => order.Items).GroupBy(item => item.NameSnapshot).OrderByDescending(group => group.Sum(item => item.Quantity)).Take(5).Select(group => new RankedCatalogItem(group.Key, group.Sum(item => item.Quantity))).ToList();
        var quoted = Orders.Count(order => order.Status == OrderStatus.Quoted || order.Status == OrderStatus.Approved || order.Status == OrderStatus.InProduction || order.Status == OrderStatus.Ready || order.Status == OrderStatus.Delivered);
        var approved = Orders.Count(order => order.Status is OrderStatus.Approved or OrderStatus.InProduction or OrderStatus.Ready or OrderStatus.Delivered);
        return new DashboardResponse(received, expected, overdue, periodOrders.Count, periodOrders.Count == 0 ? 0m : periodOrders.Average(order => order.Total), periodOrders.Select(order => order.ClientId).Distinct().Count(), statusCounts, sellers, quoted == 0 ? 0m : Math.Round((decimal)approved / quoted * 100, 1));
    }
}

public static class ReceiptPdf
{
    public static byte[] Create(Order order, Client client)
    {
        var lines = new List<string> { "RECIBO NAO FISCAL", $"Pedido: {order.Number}", $"Cliente: {client.Name}", $"Data: {order.CreatedOn:dd/MM/yyyy}", "" };
        lines.AddRange(order.Items.Select(item => $"{item.Quantity}x {item.NameSnapshot} - R$ {item.UnitPrice * item.Quantity:0.00}"));
        lines.AddRange(["", $"Total: R$ {order.Total:0.00}", $"Pago: R$ {order.Paid:0.00}", $"Saldo: R$ {order.Balance:0.00}", "", "Este documento nao possui valor fiscal."]);
        var content = "BT\n/F1 12 Tf\n50 790 Td\n14 TL\n" + string.Join("\n", lines.Select(line => $"({Escape(line)}) Tj\nT*")) + "\nET";
        var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream" };
        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Length; index++) { offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString())); builder.Append($"{index + 1} 0 obj\n{objects[index]}\nendobj\n"); }
        var xref = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        for (var index = 1; index < offsets.Count; index++) builder.Append($"{offsets[index]:D10} 00000 n \n");
        builder.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string Escape(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
