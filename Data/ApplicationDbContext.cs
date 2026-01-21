using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IT_Asset_Management_System.Models;

namespace IT_Asset_Management_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<BaseAsset> Assets { get; set; }
        public DbSet<Computer> Computers { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ServerApplication> ServerApplications { get; set; }
        public DbSet<AssetServerApplication> AssetServerApplications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ITSupportTicket> ITSupportTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Table-Per-Type (TPT) Inheritance - Object-Oriented with 3NF (no duplicates)
            // BaseAsset (abstract) maps to "Assets" table with common fields + AssetType property
            // Derived types (Computer, Server, Application) map to their own tables with specific fields only
            // Each derived table shares the same PK as the base table (EF Core handles this automatically in TPT)
            
            // Base table
            modelBuilder.Entity<BaseAsset>()
                .ToTable("Assets")
                .Property(b => b.PurchasePrice)
                .HasPrecision(18, 2); // Configure decimal precision for SQL Server

            // TPT: Each derived type has its own table, sharing PK with base table
            // EF Core automatically creates the shared PK relationship in TPT
            modelBuilder.Entity<Computer>()
                .ToTable("Computers");

            modelBuilder.Entity<Server>()
                .ToTable("Servers");

            modelBuilder.Entity<Application>()
                .ToTable("Applications");

            // Legacy ServerApplication relationships (for backward compatibility)
            // Use Restrict instead of Cascade to avoid multiple cascade paths with TPT inheritance
            modelBuilder.Entity<ServerApplication>()
                .HasOne(serverApp => serverApp.Server)
                .WithMany(server => server.ServerApplications)
                .HasForeignKey(serverApp => serverApp.ServerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServerApplication>()
                .HasOne(serverApp => serverApp.Application)
                .WithMany(app => app.ServerApplications)
                .HasForeignKey(serverApp => serverApp.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // AssetServerApplication relationships (using BaseAsset polymorphic navigation)
            modelBuilder.Entity<AssetServerApplication>()
                .HasOne(asa => asa.Server)
                .WithMany()
                .HasForeignKey(asa => asa.ServerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssetServerApplication>()
                .HasOne(asa => asa.Application)
                .WithMany()
                .HasForeignKey(asa => asa.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}