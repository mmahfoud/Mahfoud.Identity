using Mahfoud.Identity.Context;
using Mahfoud.Identity.DTOs;
using Mahfoud.Identity.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Security.Claims;
namespace Mahfoud.Identity;

public static class ToDoItemEndpoints
{
    public static void MapToDoItemEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ToDoItem").WithTags(nameof(ToDoItem));

        group.MapGet("/by-list/{listId}", async Task<Results<Ok<List<ToDoItemDTO>>, UnauthorizedHttpResult>>(long listId, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            return TypedResults.Ok(await db
                .ToDoItems.Where(i => i.ToDoList!.Id == listId && i.ToDoList!.UserId == userId)
                .Select(i => new ToDoItemDTO(i.Id, i.ListId, i.Task, i.Description, i.IsCompleted, i.DueDate))
                .ToListAsync());
        })
        .WithName("GetAllToDoItems")
        .WithOpenApi();

        group.MapGet("/by-id/{id}", async Task<Results<Ok<ToDoItemDTO>, NotFound, UnauthorizedHttpResult>> (long id, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            return await db.ToDoItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.ToDoList!.UserId == userId)
                is ToDoItem r
                    ? TypedResults.Ok(new ToDoItemDTO(r.Id, r.ListId, r.Task, r.Description, r.IsCompleted, r.DueDate))
                    : TypedResults.NotFound();
        })
        .WithName("GetToDoItemById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound, UnauthorizedHttpResult>> (long id, ToDoItemDTO model, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var affected = await db.ToDoItems
                .Where(model => model.Id == id && model.ToDoList!.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.ListId, m => db.ToDoLists.First(l => l.Id == model.ListId && l.UserId == userId).Id)
                    .SetProperty(m => m.Task, model.Task)
                    .SetProperty(m => m.Description, model.Description)
                    .SetProperty(m => m.IsCompleted, model.IsCompleted)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateToDoItem")
        .WithOpenApi();

        group.MapPost("/", Results<Created<ToDoItemDTO>, UnauthorizedHttpResult>(ToDoItemDTO model, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            // Optimize this to be done in single command
            //var toDoList = db.ToDoLists
            //    .Where(l => l.Id == model.ListId && l.UserId == userId)
            //    .Select(l => new ToDoItem
            //    {
            //        ListId = l.Id,
            //        Task = model.Task,
            //        Description = model.Description,
            //        IsCompleted = model.IsCompleted
            //    });
            //var added = await db.ToDoItems.AddAsync(toDoList.First());
            //await db.SaveChangesAsync();
            var toAdd = new ToDoItem() { Task = model.Task, Description = model.Description, IsCompleted = model.IsCompleted, ListId = model.ListId, DueDate = model.DueDate };
            // Get the entity type metadata for ToDoItem
            var itemsEntityType = db.Model.FindEntityType(typeof(ToDoItem))!;
            var listsEntityType = db.Model.FindEntityType(typeof(ToDoList))!;

            // Get the table name and schema (if any) for ToDoItem
            string itemsTableName = itemsEntityType.GetTableName()!;
            var schema = itemsEntityType.GetSchema();
            var itemsFullTableName = schema != null ? $"{schema}.{itemsTableName}" : itemsTableName;

            // Get the table name and schema (if any) for ToDoList
            var listsTableName = listsEntityType.GetTableName();
            schema = itemsEntityType.GetSchema();
            var listsFullTableName = schema != null ? $"{schema}.{listsTableName}" : listsTableName;

            // Get all properties that are not shadow, key, or concurrency tokens
            var properties = itemsEntityType.GetProperties()
                .Where(p => !p.IsShadowProperty() && !p.IsKey() && !p.IsConcurrencyToken)
                .ToList();

            // Build column names and parameter placeholders
            var columnNames = string.Join(", ", properties.Select(p => p.GetColumnName(StoreObjectIdentifier.Table(itemsFullTableName, schema))));
            var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            // Create the parameters for the query
            var parameters = properties.Select(p => new Npgsql.NpgsqlParameter($"@{p.Name}", p.PropertyInfo!.GetValue(toAdd) ?? DBNull.Value)).ToList();

            // Add userId and listId parameters
            parameters.Add(new Npgsql.NpgsqlParameter("@userId", userId));
            parameters.Add(new Npgsql.NpgsqlParameter("@listId", model.ListId));

            var userIdColumnName = listsEntityType.GetProperty("UserId").GetColumnName();
            var idColumnName = listsEntityType.GetProperty("Id").GetColumnName();

            // Construct the SQL query with parameter placeholders
            var sql = $@"INSERT INTO {itemsFullTableName} ({columnNames}) SELECT {parameterNames} FROM {listsFullTableName} l WHERE l.{userIdColumnName} = @userId AND l.{idColumnName} = @listId RETURNING *";

            // Execute the query with interpolated values
            var added = db.ToDoItems.FromSqlRaw(sql, [.. parameters]).AsEnumerable().FirstOrDefault();
            if (added is null) return TypedResults.Unauthorized();
            model = model with { Id = added.Id };
            return TypedResults.Created($"/api/ToDoItem/by-id/{model.Id}", model);
        })
        .WithName("CreateToDoItem")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound, UnauthorizedHttpResult>> (long id, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var affected = await db.ToDoItems
                .Where(i => i.Id == id && i.ToDoList!.UserId == userId)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteToDoItem")
        .WithOpenApi();
    }
}
