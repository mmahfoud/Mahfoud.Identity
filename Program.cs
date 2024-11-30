using Mahfoud.Identity;
using Mahfoud.Identity.Context;
using Mahfoud.Identity.DTOs;
using Mahfoud.Identity.Entities;
using Mahfoud.Identity.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()//.EnableSensitiveDataLogging()
    options.UseSqlServer(connectionString)
);

builder.Services.AddIdentity<User, IdentityRole<long>>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddTokenProvider<DataProtectorTokenProvider<User>>(TokenOptions.DefaultProvider);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.Configure<IdentityOptions>(options =>
{
    // Default Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.TagActionsBy(api =>
    {
        var tags = new List<string>();

        tags.AddRange(api.ActionDescriptor.EndpointMetadata.OfType<TagsAttribute>().SelectMany(x => x.Tags));
        var attribute = api.ActionDescriptor.EndpointMetadata.OfType<ApiExplorerSettingsAttribute>().FirstOrDefault();
        tags.Add(attribute?.GroupName ?? api.GroupName ?? "Default");

        return tags;
    });
});
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddTransient<IEmailSender, SimpleEmailSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.MapToDoEndpoints();

app.MapMethods("/show", ["GET", "POST", "DELETE", "PUT", "HEAD"], (IEnumerable<EndpointDataSource> sources) =>
{
    var endpoints = sources.SelectMany(es => es.Endpoints)
    .Select(e => new
    {
        Summaries = string.Join(", ", e.Metadata.OfType<EndpointSummaryAttribute>().Select(x => x.Summary)),
        Order = (e as RouteEndpoint)?.Order,
        DisplayName = e.DisplayName,
        Descriptions = string.Join(", ", e.Metadata.OfType<IEndpointDescriptionMetadata>().Select(x => x.Description)),
        Methods = string.Join("\n", e.Metadata.OfType<HttpMethodMetadata>().Select(x => string.Join(", ", x.HttpMethods))),
        Pattern = (e as RouteEndpoint)?.RoutePattern.RawText,
        Names = string.Join(", ", e.Metadata.OfType<EndpointNameMetadata>().Select(x => x.EndpointName)),
        Tags = string.Join("\n", e.Metadata.OfType<TagsAttribute>().Select(x => string.Join(", ", x.Tags))),
        GroupNames = string.Join(", ", e.Metadata.OfType<EndpointGroupNameAttribute>().Select(x => x.EndpointGroupName))
    });

    return Results.Json(endpoints);
})
.WithName("ShowAll")
.WithTags("TryThis", "AndTryThat")
.WithDisplayName("Mahfoud")
.WithDescription("Please read this description!")
//.WithGroupName("Standard")
.WithOrder(-1);

app.MapGet("/me", async Task<Ok<ProfileDTO>>(UserManager<User> _userManager, ClaimsPrincipal cp) =>
{
    var user = await _userManager.GetUserAsync(cp);
    var obj = new ProfileDTO(
        user?.Id,
        user is null ? "Anonymous" : user?.FirstName + " " + user?.LastName,
        cp.Claims.Select(x => $"{x.Type}: {x.Value}").ToList()
        );
    return TypedResults.Ok(obj);
}).WithName("Me").WithTags("Account").RequireAuthorization();

app.MapPost("/login", async Task<Results<Ok, RedirectHttpResult, UnauthorizedHttpResult>> (LoginDTO input, HttpContext ctx, SignInManager<User> _signInManager) =>
{
    var result = await _signInManager.PasswordSignInAsync(input.UserName, input.Password, false, lockoutOnFailure: true);
    if (result.Succeeded)
    {
        return TypedResults.Ok();
    }

    if (result.RequiresTwoFactor) return TypedResults.Redirect("./LoginWith2fa");

    // if (result.IsLockedOut) return TypedResults.Unauthorized();

    return TypedResults.Unauthorized();
}).WithName("LoginDTO");

app.MapPost("/register", async Task<Results<Ok, RedirectHttpResult, CreatedAtRoute<string>, ProblemHttpResult>> (
    RegistrationDTO input,
    HttpContext ctx,
    LinkGenerator _urlHelper,
    UserManager<User> _userManager,
    IUserStore<User> _userStore,
    SignInManager<User> _signInManager,
    IEmailSender _emailSender
    ) =>
{
    var user = new User() { FirstName = input.FirstName, LastName = input.LastName };
    await _userStore.SetUserNameAsync(user, input.EmailAddress, CancellationToken.None);
    if (_userStore is IUserEmailStore<User> _emailStore)
    {
        await _emailStore.SetEmailAsync(user, input.EmailAddress, CancellationToken.None);
    }
    var result = await _userManager.CreateAsync(user, input.Password);

    if (result.Succeeded)
    {
        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = _urlHelper.GetUriByRouteValues(ctx, "VerifyEmail", new { userId, code });

        await _emailSender.SendEmailAsync(input.EmailAddress, "Confirm your email",
            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");
        return TypedResults.CreatedAtRoute(userId, "Me", null);
    }
    var errs = string.Join("\n", result.Errors.Select(e => $"{e.Code}: {e.Description}").ToArray());
    return TypedResults.Problem(new ProblemDetails()
    {
        Title = "Registration error",
        Status = (int)HttpStatusCode.BadRequest,
        Detail = errs
    });
}).WithName("Register").WithTags("Account");

app.MapGet("/verify-email/{UserId}&{Code}", async Task<Results<Ok<string>, NotFound, ProblemHttpResult>> (
    long UserId, string Code, [FromServices]UserManager<User> _userManager) =>
{
    var user = await _userManager.FindByIdAsync(UserId.ToString());
    if (user == null) return TypedResults.NotFound();

    var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Code));
    var result = await _userManager.ConfirmEmailAsync(user, code);
    return result.Succeeded ? TypedResults.Ok("Thank you for confirming your email.") : TypedResults.Problem("Error confirming your email.");
}).WithName("VerifyEmail").WithTags("Account");

app.MapToDoListEndpoints();

app.MapToDoItemEndpoints();

app.MapControllers();
//app.UseRouting();

//app.UseEndpoints(b =>
//{
//    // Retrieve the EndpointDataSource service to enumerate endpoints
//    var endpointDataSources = b.DataSources;

//    Console.WriteLine("Enumerating all registered endpoints:");

//    foreach (var endpoint in endpointDataSources.SelectMany(x => x.Endpoints))
//    {
//        Console.WriteLine($"==============\n***{endpoint}:");
//        if (endpoint is RouteEndpoint routeEndpoint)
//        {

//            // Print the route pattern
//            Console.WriteLine($"\tPattern: {routeEndpoint.RoutePattern.RawText}\n\tOrder: {routeEndpoint.Order}\n\tDisplay Name: {routeEndpoint.DisplayName}");

//            // Print metadata (e.g., HTTP methods, custom metadata, etc.)
//            var httpMethods = routeEndpoint.Metadata
//                .OfType<HttpMethodMetadata>()
//                .FirstOrDefault()?.HttpMethods;
//            Console.WriteLine($"\tHTTP Methods: {string.Join(", ", httpMethods ?? new List<string>())}");
//        }
//    }
//});

// await Task.Delay(1000);
app.Use(next =>
{
    var scope = app.Services.CreateScope();
    var endpointSources = scope.ServiceProvider.GetRequiredService<IEnumerable<EndpointDataSource>>();

    Console.WriteLine("Enumerating all registered endpoints:");

    foreach (var endpoint in endpointSources.SelectMany(x => x.Endpoints))
    {
        Console.WriteLine($"==============\n***{endpoint}:");
        if (endpoint is RouteEndpoint routeEndpoint)
        {

            // Print the route pattern
            Console.WriteLine($"\tPattern: {routeEndpoint.RoutePattern.RawText}\n\tOrder: {routeEndpoint.Order}\n\tDisplay Name: {routeEndpoint.DisplayName}");

            // Print metadata (e.g., HTTP methods, custom metadata, etc.)
            var httpMethods = routeEndpoint.Metadata
                .OfType<HttpMethodMetadata>()
                .FirstOrDefault()?.HttpMethods;
            Console.WriteLine($"\tHTTP Methods: {string.Join(", ", httpMethods ?? new List<string>())}");
        }
    }
    return next;
});

await app.RunAsync();
