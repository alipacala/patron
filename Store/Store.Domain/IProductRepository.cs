namespace Store.Domain
{
    public interface IProductRepository
    {
        Task<Product?> GetProductById(int id);
        Task<bool> UpdateProduct(Product product);
    }
}