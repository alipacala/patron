namespace Store.Domain.Tests
{
    public class ProductRepositoryTests
    {
        [Test]
        public async Task UpdateProduct_Should_Update_Product_If_Not_Modified_During_Delay()
        {
            // Arrange
            var repository = new InMemoryProductRepository();
            var productToUpdate = new Product(1, "Updated Product", 29.99m);

            // Act
            var task = Task.Run(async () =>
            {
                // Simular un retraso
                await Task.Delay(2000);

                // Actualizar el producto
                var isUpdated = await repository.UpdateProduct(productToUpdate);

                // Assert
                Assert.True(isUpdated);

                // Comprobar que el producto se ha actualizado correctamente
                var updatedProduct = await repository.GetProductById(1);
                Assert.NotNull(updatedProduct);
                Assert.AreEqual(1, updatedProduct.Id);
                Assert.AreEqual("Updated Product", updatedProduct.Name);
                Assert.AreEqual(29.99m, updatedProduct.Price);
            });

            await task;
        }

        [Test]
        public async Task UpdateProduct_Should_Throw_Exception_If_Modified_During_Delay()
        {
            // Arrange
            var repository = new InMemoryProductRepository();
            var productToUpdate = new Product(1, "Updated Product", 29.99m);

            // Act
            var task = Task.Run(async () =>
            {
                // Simular un retraso
                await Task.Delay(2000);

                // Otro usuario actualiza el producto
                var anotherUserProduct = await repository.GetProductById(1);
                anotherUserProduct.Name = "Modified Product";
                await repository.UpdateProduct(anotherUserProduct);

                // Assert
                Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    // Tratar de actualizar el producto
                    await repository.UpdateProduct(productToUpdate);
                });
            });

            await task;
        }
    }
}