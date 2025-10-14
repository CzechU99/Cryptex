var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Cryptex.Services.EncryptionService>();
builder.Services.AddSingleton<Cryptex.Services.RateLimitService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
