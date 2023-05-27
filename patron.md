# SESION DE LABORATORIO N° 02: PATRONES DE DISEÑO ESTRUCTURALES

## DESARROLLO

### Patron EAA: Optimistic Offline Lock

![Optimistic Offline Lock - Sequence Diagram Example](https://martinfowler.com/eaaCatalog/OptimisticSketch.gif)

El patrón Optimistic Offline Lock es una técnica utilizada para prevenir conflictos entre transacciones comerciales concurrentes al detectar un conflicto y deshacer la transacción.

Este patrón es especialmente útil cuando una transacción comercial se ejecuta a lo largo de una serie de transacciones del sistema. Una vez que se sale de los límites de una sola transacción del sistema, no se puede depender únicamente del administrador de la base de datos para garantizar que la transacción comercial deje los datos de los registros en un estado consistente. La integridad de los datos está en riesgo cuando dos sesiones comienzan a trabajar en los mismos registros y es posible que se produzcan actualizaciones perdidas. Además, cuando una sesión edita datos que otra está leyendo, es probable que se produzca una lectura inconsistente.

El Optimistic Offline Lock resuelve este problema al validar que los cambios que una sesión está a punto de confirmar no entren en conflicto con los cambios de otra sesión. Una validación exitosa antes de confirmar es, en cierto sentido, obtener un bloqueo que indica que está permitido realizar los cambios en los datos del registro. Siempre que la validación y las actualizaciones ocurran dentro de una sola transacción del sistema, la transacción comercial mostrará consistencia.

A diferencia del Pessimistic Offline Lock que asume que la probabilidad de conflicto entre sesiones es alta y, por lo tanto, limita la concurrencia del sistema, el Optimistic Offline Lock asume que la probabilidad de conflicto es baja. La expectativa de que no es probable que ocurra un conflicto entre sesiones permite que múltiples usuarios trabajen con los mismos datos al mismo tiempo.

1. Iniciar la aplicación Powershell o Windows Terminal en modo administrador
2. Ejecutar el siguiente comando para crear una nueva solución

    ```powershell
    dotnet new sln -o Store
    ```

3. Acceder a la solución creada y ejecutar el siguiente comando para crear una nueva librería de clases y adicionarla a la solución actual.

    ```powershell
    cd Store
    dotnet new classlib -o Store.Domain
    dotnet sln add ./Store.Domain/Store.Domain.csproj
    ```

4. Ejecutar el siguiente comando para crear un nuevo proyecto de pruebas y adicionarla a la solución actual

    ```powershell
    dotnet new nunit -o Store.Domain.Tests
    dotnet sln add ./Store.Domain.Tests/Store.Domain.Tests.csproj
    dotnet add ./Store.Domain.Tests/Store.Domain.Tests.csproj reference ./Store.Domain/Store.Domain.csproj
    ```

5. Iniciar Visual Studio Code (VS Code) abriendo el folder de la solución como proyecto. En el proyecto `Store.Domain`, si existe un archivo `Class1.cs` proceder a eliminarlo. Asimismo en el proyecto `Store.Domain.Tests` si existiese un archivo `UnitTest1.cs`, también proceder a eliminarlo.

6. Primero se necesita definir la clase `Product` que servirá para modificar sus valores. Creamos un archivo `Product.cs` en `Store.Domain`

    ```C#
    namespace Store.Domain
    {
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public DateTime LastModified { get; set; }

            public Product(int id, string name, decimal price)
            {
                Id = id;
                Name = name;
                Price = price;
                LastModified = DateTime.UtcNow;
            }
        }
    }
    ```

7. Creamos una interface `IProductRepository` en `Store.Domain` como buena práctica.

   > IProductRepository.cs

    ```C#
    namespace Store.Domain
    {
        public interface IProductRepository
        {
            Task<Product?> GetProductById(int id);
            Task<bool> UpdateProduct(Product product);
        }
    }
    ```

8. Ahora proceder a implementar la clase `InMemoryProductRepository` en `Store.Domain` que es responsable de acceder a los productos de la persistencia que en este caso solo está en memoria. Tiene métodos para obtener un producto por ID y actualizar un producto. El patrón indica que no se debe modificar el registro si se ha modificado durante el lapso de tiempo de haber estado offline, para lo cual se hace uso de la propiedad `LastModified` para verificar que sea el mismo después de consultar el registro a modificar y antes de realizar la actualización del registro. Si no se ha modificado, entonces se modifica el registro, en caso contrario, se lanza una excepción que indica que se ha modificado el registro por otro usuario.

    > InMemoryProductRepository.cs

    ```C#
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
    ```

9. Seguidamente crear la clase de pruebas `ProductTests` en `Store.Domain.Tests` que permitirá probar la funcionalidad. Esta clase contiene dos pruebas:

    * Probar que si no se modifica el registro en el lapso del retraso, se puede actualizar el registro con normalidad.
    * Probar que si se modifica el registro en el lapso del retraso, no se puede actualizar el registro y se lanza una excepción.

    > ProductTests.cs

    ```C#
    namespace Store.Domain.Tests
    {
        public class ProductTests
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
    ```

10. Ahora necesitamos comprobar las pruebas construidas para eso abrir un terminal en VS Code (CTRL + Ñ) o vuelva al terminal anteriormente abierto, y ejecutar el comando:

```Bash
dotnet test --collect:"XPlat Code Coverage"
```

11. Los resultados de las pruebas son correctos al 100%:

```Bash
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 4 s - Store.Domain.Tests.dll (net7.0)
```
