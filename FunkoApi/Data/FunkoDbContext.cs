using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Data;

public class FunkoDbContext : DbContext
{
    public FunkoDbContext(DbContextOptions<FunkoDbContext> options) : base(options) { }
    
    public DbSet<Funko> Funkos => Set<Funko>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
}