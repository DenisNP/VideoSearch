using VideoSearch.VideoDescriber;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// basic infrastructure
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// application
builder.Services.AddVideoDescriber(Environment.GetEnvironmentVariable("VIDEO_DESCRIBER_URL"));

// build
WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();