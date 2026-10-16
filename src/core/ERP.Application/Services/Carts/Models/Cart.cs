namespace ERP.Application.Services.Carts.Models
{
    public class Cart
    {
        public string CustomerId { get; private set; } = default!;

        private readonly List<CartItem> _items = [];
        public IReadOnlyList<CartItem> Items => _items;

        public Cart(string customerId)
        {
            CustomerId = customerId;
        }

        public void AddItem(string itemId, string itemName, decimal price, int quantity, string pictureUri)
        {
            _items.Add(new CartItem(this, itemId, itemName, price, quantity, pictureUri));
        }
    }
}
