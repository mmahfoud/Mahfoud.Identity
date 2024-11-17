using Mahfoud.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Mahfoud.Identity.Context;

public partial class ApplicationDbContext : IdentityDbContext<User, IdentityRole<long>, long>
{
    public static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public override DbSet<User> Users { get; set; } = null!;
    public DbSet<ToDoList> ToDoLists { get; set; } = null!;
    public DbSet<ToDoItem> ToDoItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region IdentityDbContext customizations
        modelBuilder.Entity<User>(b =>
        {
            // b.ToTable("users");
            b.Property(u => u.FirstName).IsRequired();
            b.Property(u => u.LastName).IsRequired();
            b.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            b.HasKey(p => p.Id);
            b.HasAlternateKey(u => u.NormalizedUserName);
            b.HasAlternateKey(u => u.NormalizedEmail);
            b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

            b.HasMany<IdentityUserClaim<long>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            b.HasMany<IdentityUserRole<long>>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
            b.HasMany<IdentityUserToken<long>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
            b.HasMany<IdentityUserLogin<long>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
        });

        modelBuilder.Entity<IdentityUserClaim<long>>(b =>
        {
            // b.ToTable("user_claims");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.UserId);
        });

        modelBuilder.Entity<IdentityUserLogin<long>>(b =>
        {
            // b.ToTable("user_logins");
            b.HasKey(p => new { p.UserId, p.LoginProvider, p.ProviderKey });
            b.HasIndex(p => p.UserId);
        });

        modelBuilder.Entity<IdentityUserToken<long>>(b =>
        {
            // b.ToTable("user_tokens");
            b.HasKey(p => new { p.UserId, p.LoginProvider, p.Name });
            b.HasIndex(p => p.UserId);
        });

        modelBuilder.Entity<IdentityRole<long>>(b =>
        {
            // b.ToTable("roles");
            b.HasKey(p => p.Id);
            b.HasAlternateKey(r => r.NormalizedName);
            b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            b.HasMany<IdentityUserRole<long>>().WithOne().HasForeignKey(us => us.RoleId).IsRequired();
            b.HasMany<IdentityRoleClaim<long>>().WithOne().HasForeignKey(uc => uc.RoleId).IsRequired();
        });

        modelBuilder.Entity<IdentityUserRole<long>>(b =>
        {
            // b.ToTable("user_roles");
            b.HasKey(p => new { p.UserId, p.RoleId });
            b.HasIndex(p => p.UserId);
            b.HasIndex(p => p.RoleId);
        });

        modelBuilder.Entity<IdentityRoleClaim<long>>(b =>
        {
            // b.ToTable("role_claims");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.RoleId);
        });
        #endregion
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public string? Provider
    {
        get
        {
            return this.Database.ProviderName;
        }
    } 
}