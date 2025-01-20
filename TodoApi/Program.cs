using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// הגדרת DbContext
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
        new MySqlServerVersion(new Version(8, 0, 32))));

// הוספת שירותי CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// הוספת Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// שליפת כל המשימות
app.MapGet("/items", async (ToDoDbContext context) =>
{
    return await context.Items.ToListAsync();
});

// הוספת משימה חדשה
app.MapPost("/items", async (ToDoDbContext context, Item newItem) =>
{
    if (string.IsNullOrWhiteSpace(newItem.Name))
    {
        return Results.BadRequest("Name is required.");
    }

    context.Items.Add(newItem);
    await context.SaveChangesAsync();
    return Results.Created($"/items/{newItem.Id}", newItem);
});

// עדכון משימה
app.MapPut("/items/{id}", async (ToDoDbContext context, int id, Item updatedItem) =>
{
    var existingItem = await context.Items.FindAsync(id);
    if (existingItem is null)
    {
        return Results.NotFound();
    }

    existingItem.Name = updatedItem.Name;
    existingItem.IsComplete = updatedItem.IsComplete;
    await context.SaveChangesAsync();
    return Results.Ok(existingItem);
});

// מחיקת משימה
app.MapDelete("/items/{id}", async (ToDoDbContext context, int id) =>
{
    var item = await context.Items.FindAsync(id);
    if (item is null)
    {
        return Results.NotFound();
    }

    context.Items.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
});

// הוספת נתוני דוגמה
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
    dbContext.Database.EnsureCreated();
    if (!dbContext.Items.Any())
    {
        dbContext.Items.AddRange(
            new Item { Name = "Task 1", IsComplete = false },
            new Item { Name = "Task 2", IsComplete = true }
        );
        dbContext.SaveChanges();
    }
}

app.Run();