namespace ERP.Application.Services.Carts.Models
{
    public class CartItem
    {
        public Cart Cart { get; private set; } = default!;
        public string ItemId { get; private set; } = default!;
        public string ItemName { get; private set; } = default!;
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }
        public string PictureUri { get; private set; } = default!;

        public CartItem(Cart cart, string itemId, string itemName, decimal price, int quantity, string pictureUri)
        {
            ArgumentNullException.ThrowIfNull(cart, nameof(cart));
            Cart = cart;
            ItemId = itemId;
            ItemName = itemName;
            Price = price;
            Quantity = quantity;
            PictureUri = pictureUri;
        }
    }
}
