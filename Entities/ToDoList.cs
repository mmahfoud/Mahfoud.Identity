namespace Mahfoud.Identity.Entities;

public class ToDoList
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public virtual User? User { get; set; }
    public virtual ICollection<ToDoItem> Items { get; set; } = [];
}
