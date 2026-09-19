namespace LinhaELaco.Api;

public static class DomainRules
{
    public static bool IsValidCatalog(CreateCatalogItemRequest item) =>
        !string.IsNullOrWhiteSpace(item.Name) && item.BasePrice >= 0 && item.EstimatedDays >= 0 &&
        ((item.Kind == CatalogKind.Service && item.Service is not null && item.Garment is null) ||
         (item.Kind == CatalogKind.Garment && item.Garment is not null && item.Service is null));

    public static OrderStatus? NextOrderStatus(OrderStatus status) => status switch
    {
        OrderStatus.Quoted => OrderStatus.Approved,
        OrderStatus.Approved => OrderStatus.InProduction,
        OrderStatus.InProduction => OrderStatus.Ready,
        OrderStatus.Ready => OrderStatus.Delivered,
        _ => null
    };

    public static InstallmentStatus InitialInstallmentStatus(DateOnly dueDate, DateOnly today) => dueDate < today ? InstallmentStatus.Overdue : InstallmentStatus.Open;
}