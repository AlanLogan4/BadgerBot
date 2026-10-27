using Bot.Core;
var builder = WebApplication.CreateBuilder(args);
var botService = new BotService();
builder.Services.AddSingleton(botService);

var app = builder.Build();


app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/tactic", (BotService bot) => new {tactic = bot.currentTactic.ToString()}).WithName("GetTactic");
app.MapPost("/api/tactic/{newTactic}", (string newTactic, BotService bot) =>
{
    if(Enum.TryParse<Tactics>(newTactic, true, out var tactic))
    {
      bot.currentTactic = tactic;
    }
    return new {tactic = bot.currentTactic.ToString()}; 
}).WithName("ChangeTactic");

app.Run();