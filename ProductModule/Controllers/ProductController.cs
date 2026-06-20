using Microsoft.AspNetCore.Mvc;
using ProductModule.Models;

namespace ProductModule.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private static readonly List<Product> _products = new();
    private static int _nextId = 1;

    [HttpGet]
    public IEnumerable<Product> GetAll() => _products;

    [HttpGet("{id}")]
    public ActionResult<Product> GetById(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();
        return product;
    }

    [HttpPost]
    public ActionResult<Product> Create(Product product)
    {
        product.Id = _nextId++;
        _products.Add(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Product product)
    {
        var existingProduct = _products.FirstOrDefault(p => p.Id == id);
        if (existingProduct == null) return NotFound();

        existingProduct.ProductName = product.ProductName;
        existingProduct.ProductDescription = product.ProductDescription;
        existingProduct.NumberUnits = product.NumberUnits;
        existingProduct.Reviews = product.Reviews;
        existingProduct.MediaLinks = product.MediaLinks;
        existingProduct.Color = product.Color;
        existingProduct.Sizes = product.Sizes;

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();
        _products.Remove(product);
        return NoContent();
    }
}
