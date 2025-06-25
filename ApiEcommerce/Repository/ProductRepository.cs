using System;
using ApiEcommerce.Models;
using ApiEcommerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace ApiEcommerce.Repository;

public class ProductRepository : IProductRepository
{
    public readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }
    public bool BuyProduct(string name, int quantity)
    {

        Product? product = _db.Products.FirstOrDefault(p => p.Name.ToLower().Trim() == name.ToLower().Trim());
        if (product != null && product.Stock > quantity)
        {
            product.Stock -= quantity;
            return Save();
        }

        return false;
    }

    public bool CreateProduct(Product product)
    {
        product.CreationDate = DateTime.Now;
        product.UpdateDate = DateTime.Now;

        _db.Products.Add(product);
        return Save();
    }

    public bool DeleteProduct(Product product)
    {
        _db.Products.Remove(product);
        return Save();
    }

    public Product? GetProduct(int id)
    {
        return _db.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
    }

    public ICollection<Product> GetProducts()
    {
        return _db.Products.OrderBy(p => p.Name).ToList();
    }

    public ICollection<Product> GetProductsForCategory(int categoryId)
    {
        // revisar si no funciona validar que el categoryId existe y retornar instanacia de product

        return _db.Products.Include(p => p.Category).Where(p => p.CategoryId == categoryId).OrderBy(p => p.Name).ToList();
    }

    public bool ProductExists(int id)
    {
        return _db.Products.Any(p => p.Id == id);
    }

    public bool ProductExists(string name)
    {
        return _db.Products.Any(p => p.Name.ToLower().Trim() == name.ToLower().Trim());
    }

    public bool Save()
    {
        return _db.SaveChanges() >= 0;
    }

    public ICollection<Product> SearchProducts(string searchTerm)
    {
        IQueryable<Product> query = _db.Products;
        var searchTermLowered = searchTerm.ToLower().Trim();
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Include(p => p.Category)
                .Where(
                    p => p.Name.ToLower().Trim().Contains(searchTermLowered) ||
                    p.Description.ToLower().Trim().Contains(searchTermLowered)
                );
        }

        return query.OrderBy(p => p.Name).ToList();
    }

    public bool UpdateProduct(Product product)
    {
        product.UpdateDate = DateTime.Now;
        _db.Products.Update(product);

        return Save();
    }
}
