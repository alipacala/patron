namespace Store.Domain
{
    public class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> products;

        public InMemoryProductRepository()
        {
            products = new List<Product>
            {
                new Product(1, "Product 1", 9.99m),
                new Product(2, "Product 2", 19.99m),
                new Product(3, "Product 3", 29.99m)
            };
        }

        public async Task<Product?> GetProductById(int id)
        {
            Product? product = products.SingleOrDefault(p => p.Id == id);

            if (product == null) return null;
            return product;
        }

        public async Task<bool> UpdateProduct(Product product)
        {
            // Revisa si el producto ha sido modificado por otro usuario.
            Product? existingProduct = products.SingleOrDefault(p => p.Id == product.Id);
            if (existingProduct != null && existingProduct.LastModified > product.LastModified)
            {
                throw new InvalidOperationException("El producto ha sido modificado por otro usuario.");
            }

            // Actualiza el producto en la lista.
            if (existingProduct != null)
            {
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.LastModified = DateTime.UtcNow;

                return true;
            }

            // Si no se encuentra el producto, devuelve false.
            return false;
        }
    }
}