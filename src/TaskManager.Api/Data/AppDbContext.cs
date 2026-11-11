using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).IsRequired().HasMaxLength(200);
            entity.Property(task => task.Description).HasMaxLength(2000);
            entity.Property(task => task.Status).IsRequired();
            entity.Property(task => task.Priority).IsRequired();
            entity.HasIndex(task => task.Status);
            entity.HasIndex(task => task.DueDate);
        });
    }
}
