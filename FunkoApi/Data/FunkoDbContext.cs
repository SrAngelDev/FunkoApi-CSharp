﻿using System.Diagnostics.CodeAnalysis;
using FunkoApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Data;

[ExcludeFromCodeCoverage]
public class FunkoDbContext(DbContextOptions<FunkoDbContext> options) 
    : IdentityDbContext<User, IdentityRole<long>, long>(options)
{
    public DbSet<Funko> Funkos => Set<Funko>();
    public DbSet<Categoria> Categorias => Set<Categoria>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Funko>(entity => 
        {
            entity.Property(f => f.Nombre).IsRequired();
            entity.Property(f => f.Precio).HasColumnType("decimal(18,2)");
        });
    }
}