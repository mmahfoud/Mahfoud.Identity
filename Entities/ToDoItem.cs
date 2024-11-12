using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mahfoud.Identity.Entities;

[EntityTypeConfiguration(typeof(ItemConfiguration))]
public class ToDoItem
{
    public long Id { get; set; }
    public long ListId { get; set; }
    public required string Task { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTimeOffset DueDate { get; set; }

    public virtual ToDoList? ToDoList { get; set; }
}

public class ItemConfiguration : IEntityTypeConfiguration<ToDoItem>
{
    public void Configure(EntityTypeBuilder<ToDoItem> builder)
    {
        builder.HasOne(i => i.ToDoList).WithMany(l => l.Items).HasForeignKey(i => i.ListId).IsRequired(true);
    }
}