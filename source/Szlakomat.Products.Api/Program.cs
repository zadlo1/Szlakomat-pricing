using Szlakomat.Pricing.Infrastructure;
using Szlakomat.Products.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProductModule();
builder.Services.AddPricingModule();

var app = builder.Build();
app.MapControllers();
app.Run();
