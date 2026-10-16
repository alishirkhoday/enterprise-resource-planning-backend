namespace ERP.Application.UseCases.Modules.OrderManagement.DTOs
{
    /// <summary>What a caller sends to place an order.</summary>
    public record PlaceOrderDto
    {
        public Guid CustomerId { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public IReadOnlyList<PlaceOrderItemDto> Items { get; init; } = [];

        public PlaceOrderAddressDto ShipTo { get; init; } = default!;

        /// <summary>How long the customer has to pay before the order gives up its claim on stock.</summary>
        public int PaymentWindowMinutes { get; init; } = 30;

        public Guid? WarehouseId { get; init; }
    }

    /// <summary>
    /// One line. The price is deliberately not part of it: a caller who can name their own price
    /// can buy at zero, so only which item and how much of it come from outside.
    /// </summary>
    public record PlaceOrderItemDto
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; }
    }

    /// <summary>
    /// Where it goes and who signs for it. Copied onto the order rather than referenced, because
    /// a customer moving house must not rewrite where last year's orders were delivered.
    /// </summary>
    public record PlaceOrderAddressDto
    {
        public string ReceiverFullName { get; init; } = default!;

        public string ReceiverMobileNumber { get; init; } = default!;

        public string Country { get; init; } = default!;

        public string Province { get; init; } = default!;

        public string City { get; init; } = default!;

        public string Region { get; init; } = default!;

        public string Street { get; init; } = default!;

        public string Plaque { get; init; } = default!;

        public string PostalCode { get; init; } = default!;

        public string? Unit { get; init; }

        public Guid? CountryId { get; init; }

        public Guid? ProvinceId { get; init; }

        public Guid? CityId { get; init; }

        public string? DeliveryInstruction { get; init; }
    }
}
