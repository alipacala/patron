namespace Store.Domain
{
    public class ProductService
    {
        private readonly IProductRepository productRepository;

        public ProductService(IProductRepository productRepository)
        {
            this.productRepository = productRepository;
        }

        public async Task<Product?> GetProductById(int id)
        {
            return await productRepository.GetProductById(id);
        }

        public async Task<bool> UpdateProduct(Product product)
        {
            return await productRepository.UpdateProduct(product);
        }
    }
}