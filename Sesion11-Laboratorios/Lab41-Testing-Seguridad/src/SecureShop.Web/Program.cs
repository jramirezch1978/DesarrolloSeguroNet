var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

// Hacer la clase Program explícitamente accesible para los tests
public partial class Program { }
