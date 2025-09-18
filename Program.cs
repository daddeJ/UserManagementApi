using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


// ====================
// 🔹 Error Handling Middleware (First in pipeline)
// ====================
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception: {ex.Message}");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorResponse = new { error = "Internal server error." };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
});


// ====================
// 🔹 Authentication Middleware (Second in pipeline)
// ====================
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault();

    if (string.IsNullOrEmpty(token) || token != "Bearer my-secret-token")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized" }));
        return;
    }

    await next();
});


// ====================
// 🔹 Logging Middleware (Last in pipeline)
// ====================
app.Use(async (context, next) =>
{
    Console.WriteLine($"➡️ Request: {context.Request.Method} {context.Request.Path}");

    await next();

    Console.WriteLine($"⬅️ Response: {context.Response.StatusCode}");
});


// ====================
// 🔹 API Endpoints
// ====================
var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@techhive.com" },
    new User { Id = 2, Name = "Bob", Email = "bob@techhive.com" }
};

// GET: Retrieve all users
app.MapGet("/users", () => users);

// GET: Retrieve a specific user by ID
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// POST: Add a new user
app.MapPost("/users", (User newUser) =>
{
    newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
});

// PUT: Update an existing user
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
        return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

// DELETE: Remove a user by ID
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
        return Results.NotFound();

    users.Remove(user);
    return Results.NoContent();
});

app.Run();


// ====================
// 🔹 User Model
// ====================
record User
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
}
