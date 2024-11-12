using Microsoft.EntityFrameworkCore;
using Mahfoud.Identity.Context;
using Mahfoud.Identity.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mahfoud.Identity.DTOs;
namespace Mahfoud.Identity;

public static class ToDoListEndpoints
{
    public static void MapToDoListEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ToDoList").WithTags(nameof(ToDoList));

        group.MapGet("/", async Task<Results<Ok<List<ToDoListDTO>>, UnauthorizedHttpResult>>(ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();
            return TypedResults.Ok(await db.ToDoLists.Where(l => l.UserId == userId).Select(l => new ToDoListDTO(l.Id, l.Name, l.Description)).ToListAsync());
        })
        .WithName("GetAllToDoLists")
        .RequireAuthorization()
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<ToDoListDTO>, NotFound, UnauthorizedHttpResult>> (long id, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();
            return await db.ToDoLists.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id && model.UserId == userId)
                is ToDoList model
                    ? TypedResults.Ok(new ToDoListDTO(model.Id, model.Name, model.Description))
                    : TypedResults.NotFound();
        })
        .WithName("GetToDoListById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound, UnauthorizedHttpResult>> (long id, ToDoListDTO toDoList, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();
            var affected = await db.ToDoLists
                .Where(model => model.Id == id && model.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Name, toDoList.Name)
                    .SetProperty(m => m.Description, toDoList.Description)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateToDoList")
        .WithOpenApi();

        group.MapPost("/", async Task<Results<Created<ToDoListDTO>, UnauthorizedHttpResult>>(ToDoListDTO model, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();
            var todoList = db.ToDoLists.Add(new ToDoList() { Name = model.Name, Description = model.Description, UserId = userId.Value });
            await db.SaveChangesAsync();
            model = model with { Id = todoList.Entity.Id };
            return TypedResults.Created($"/api/ToDoList/{model.Id}", model);
        })
        .WithName("CreateToDoList")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound, UnauthorizedHttpResult>> (long id, ApplicationDbContext db, ClaimsPrincipal cp) =>
        {
            var userId = cp.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();
            var affected = await db.ToDoLists
                .Where(model => model.Id == id && model.UserId == userId)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteToDoList")
        .WithOpenApi();
    }
}
