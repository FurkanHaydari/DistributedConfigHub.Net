using DemoConsumerApp.Data;
using DemoConsumerApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoConsumerApp.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController(ProductDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await context.Products.ToListAsync();
    }
}
