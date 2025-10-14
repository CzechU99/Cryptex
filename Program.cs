var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Cryptex.Services.EncryptionService>();
builder.Services.AddSingleton<Cryptex.Services.RateLimitService>();
builder.Services.AddSingleton<Cryptex.Services.ValidationService>();
builder.Services.AddSingleton<Cryptex.Services.FileService>();
builder.Services.AddSingleton<Cryptex.Services.ExpireTimeService>();

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
