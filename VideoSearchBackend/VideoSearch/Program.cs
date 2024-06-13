using VideoSearch.Database;
using VideoSearch.Database.Abstract;
using VideoSearch.VideoDescriber;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// basic infrastructure
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// application
builder.Services.AddVideoDescriber(Environment.GetEnvironmentVariable("VIDEO_DESCRIBER_URL"));
builder.Services.AddDatabase();

// build
WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.Services.GetRequiredService<IStorage>().Init();

app.MapControllers();
app.Run();