using Microsoft.Extensions.Caching.Distributed;
using PluralsightShopping.Shared;
using System.Text.Json;
using WebPush;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.MapControllers();

var allProducts = new[]
{
    "Hiking Boots", "Tent", "Jacket", "Hiking Poles"
};

app.MapGet("/products", () =>
{
    return allProducts.Select(x => new Product() { ProductName = x });
}).WithName("GetAllProducts");

app.MapPut("/notifications/subscribe", (IDistributedCache cache, NotificationSubscription sub) =>
{
    var json = JsonSerializer.Serialize(sub);

    // in production we'd be storing to a persistent store and we'd also be using a unique identifier for the user
    // here we're just going to assume only one user is using the app
    cache.SetString("subscription", json);

    // store the subscription in the database
    return Results.Ok();
});

app.MapPost("/newcouponalert", async (IDistributedCache cache, IConfiguration config) =>
{
    string privateKey = config["Vapid:PrivateKey"];
    string publicKey = config["Vapid:PublicKey"];
    string subject = config["Vapid:Subject"];

    // grab the subscription from the cache
    var json = cache.GetString("subscription");
    if (!string.IsNullOrEmpty(json))
    {
        var notificationSubscription = JsonSerializer.Deserialize<NotificationSubscription>(json);

        await SendNotificationAsync(notificationSubscription, "New coupon available!", publicKey, privateKey, subject);

        return Results.Ok();
    }
    else
        return Results.NotFound();
});

static async Task SendNotificationAsync(NotificationSubscription subscription, string message, string publicKey, string privateKey, string subject)
{
    var pushSubscription = new PushSubscription(subscription.Url, subscription.P256dh, subscription.Auth);
    var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
    var webPushClient = new WebPushClient();
    try
    {
        var payload = JsonSerializer.Serialize(new
        {
            message,
            url = "coupon"
        });

        await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Error sending push notification: " + ex.Message);
    }
}

app.Run();
