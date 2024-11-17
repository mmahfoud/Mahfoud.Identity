namespace Mahfoud.Identity.DTOs;

public record RegistrationDTO(string FirstName, string LastName, string EmailAddress, string Password);
public record LoginDTO(string UserName, string Password);
public record ProfileDTO(long? UserId, string? DisplayName, List<string> Claims);

public record ToDoListDTO(long Id, string Name, string? Description);
public record ToDoItemDTO(long Id, long ToDoListId, string Task, string? Description, DateTimeOffset DueDate, DateTimeOffset? CompletionDate);