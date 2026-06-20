using Microsoft.AspNetCore.Mvc;
using ProductModule.Controllers;
using ProductModule.Models;

namespace RabbitMQ.Tests;

public class ProductControllerTests
{
    [Fact]
    public void Create_AddsProductAndReturnsCreated()
    {
        // Arrange
        var controller = new ProductController();
        var newProduct = new Product { ProductName = "Test Product", NumberUnits = 10 };

        // Act
        var result = controller.Create(newProduct);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var product = Assert.IsType<Product>(createdAtActionResult.Value);
        Assert.Equal("Test Product", product.ProductName);
        Assert.True(product.Id > 0);
    }

    [Fact]
    public void GetAll_ReturnsAllProducts()
    {
        // Arrange
        var controller = new ProductController();
        controller.Create(new Product { ProductName = "P1" });
        controller.Create(new Product { ProductName = "P2" });

        // Act
        var products = controller.GetAll();

        // Assert
        Assert.Contains(products, p => p.ProductName == "P1");
        Assert.Contains(products, p => p.ProductName == "P2");
    }

    [Fact]
    public void GetById_ReturnsNotFoundForInvalidId()
    {
        // Arrange
        var controller = new ProductController();

        // Act
        var result = controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void Update_UpdatesExistingProduct()
    {
        // Arrange
        var controller = new ProductController();
        var createResult = controller.Create(new Product { ProductName = "Old Name" });
        var createdProduct = (Product)((CreatedAtActionResult)createResult.Result!).Value!;

        // Act
        var updateResult = controller.Update(createdProduct.Id, new Product { ProductName = "New Name" });

        // Assert
        Assert.IsType<NoContentResult>(updateResult);
        var getResult = controller.GetById(createdProduct.Id);
        Assert.Equal("New Name", getResult.Value!.ProductName);
    }

    [Fact]
    public void Delete_RemovesProduct()
    {
        // Arrange
        var controller = new ProductController();
        var createResult = controller.Create(new Product { ProductName = "To Delete" });
        var createdProduct = (Product)((CreatedAtActionResult)createResult.Result!).Value!;

        // Act
        var deleteResult = controller.Delete(createdProduct.Id);

        // Assert
        Assert.IsType<NoContentResult>(deleteResult);
        var getResult = controller.GetById(createdProduct.Id);
        Assert.IsType<NotFoundResult>(getResult.Result);
    }
}
