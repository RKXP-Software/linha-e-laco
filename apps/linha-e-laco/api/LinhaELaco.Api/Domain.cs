namespace LinhaELaco.Api;

public enum CatalogKind { Service, Garment }
public enum OrderStatus { Quoted, Approved, InProduction, Ready, Delivered, Cancelled }
public enum InstallmentStatus { Open, Paid, Overdue }

public record Client(Guid Id, string Name, string? Cpf, string? Email, string? Phone, Address? Address, string? Notes, DateTimeOffset CreatedAt);
public record Address(string Street, string Number, string? Complement, string Neighborhood, string City, string State, string ZipCode);
public record CatalogItem(Guid Id, CatalogKind Kind, string Name, string Description, decimal BasePrice, int EstimatedDays, ServiceDetail? Service, GarmentDetail? Garment, bool Active);
public record ServiceDetail(string Unit, string Instructions);
public record GarmentDetail(string GarmentType, string RequiredMeasurements, string ProductionStages, string Materials);
public record OrderItem(Guid CatalogItemId, string NameSnapshot, CatalogKind Kind, decimal UnitPrice, int Quantity, string? Notes);
public record PaymentInstallment(Guid Id, int Number, decimal Amount, DateOnly DueDate, DateOnly? PaidAt, InstallmentStatus Status);
public record Order(Guid Id, string Number, Guid ClientId, OrderStatus Status, DateOnly CreatedOn, DateOnly? PromisedFor, IReadOnlyList<OrderItem> Items, IReadOnlyList<PaymentInstallment> Installments, string? Notes)
{
    public decimal Total => Items.Sum(item => item.UnitPrice * item.Quantity);
    public decimal Paid => Installments.Where(item => item.Status == InstallmentStatus.Paid).Sum(item => item.Amount);
    public decimal Balance => Total - Paid;
}

public record CreateClientRequest(string Name, string? Cpf, string? Email, string? Phone, Address? Address, string? Notes);
public record CreateCatalogItemRequest(CatalogKind Kind, string Name, string Description, decimal BasePrice, int EstimatedDays, ServiceDetail? Service, GarmentDetail? Garment);
public record CreateOrderItemRequest(Guid CatalogItemId, decimal? UnitPrice, int Quantity, string? Notes);
public record CreateInstallmentRequest(decimal Amount, DateOnly DueDate);
public record CreateOrderRequest(Guid ClientId, DateOnly? PromisedFor, IReadOnlyList<CreateOrderItemRequest> Items, IReadOnlyList<CreateInstallmentRequest> Installments, string? Notes);
public record ChangeOrderStatusRequest(OrderStatus Status);
public record Note(Guid Id, string Text, bool IsDone, DateTimeOffset CreatedAt);
public record CreateNoteRequest(string Text);
public record ChangeNoteStatusRequest(bool IsDone);
public record DashboardResponse(decimal Received, decimal Expected, decimal Overdue, int Orders, decimal AverageTicket, int ActiveClients, IReadOnlyDictionary<string, int> OrdersByStatus, IReadOnlyList<RankedCatalogItem> BestSellers, decimal QuoteConversion);
public record RankedCatalogItem(string Name, int Quantity);
