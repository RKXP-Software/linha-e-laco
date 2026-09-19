using LinhaELaco.Api;

namespace LinhaELaco.Api.Tests;

public class DomainRulesTests
{
    [Fact]
    public void Service_catalog_item_requires_service_details_only()
    {
        var valid = new CreateCatalogItemRequest(CatalogKind.Service, "Ajuste", "", 45m, 2, new ServiceDetail("peça", "Provar"), null);
        var invalid = valid with { Service = null };

        Assert.True(DomainRules.IsValidCatalog(valid));
        Assert.False(DomainRules.IsValidCatalog(invalid));
    }

    [Fact]
    public void Garment_catalog_item_requires_garment_details_only()
    {
        var valid = new CreateCatalogItemRequest(CatalogKind.Garment, "Vestido", "", 480m, 20, null, new GarmentDetail("Vestido", "Busto", "Corte", "Tecido"));
        var invalid = valid with { Service = new ServiceDetail("peça", "") };

        Assert.True(DomainRules.IsValidCatalog(valid));
        Assert.False(DomainRules.IsValidCatalog(invalid));
    }

    [Theory]
    [InlineData(OrderStatus.Quoted, OrderStatus.Approved)]
    [InlineData(OrderStatus.Approved, OrderStatus.InProduction)]
    [InlineData(OrderStatus.InProduction, OrderStatus.Ready)]
    [InlineData(OrderStatus.Ready, OrderStatus.Delivered)]
    public void Active_order_statuses_advance_in_the_expected_sequence(OrderStatus current, OrderStatus expected) => Assert.Equal(expected, DomainRules.NextOrderStatus(current));

    [Theory]
    [InlineData(2026, 9, 18, InstallmentStatus.Overdue)]
    [InlineData(2026, 9, 19, InstallmentStatus.Open)]
    [InlineData(2026, 9, 20, InstallmentStatus.Open)]
    public void Installment_is_overdue_only_before_its_due_date(int year, int month, int day, InstallmentStatus expected) => Assert.Equal(expected, DomainRules.InitialInstallmentStatus(new DateOnly(year, month, day), new DateOnly(2026, 9, 19)));
}