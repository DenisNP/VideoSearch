using VideoSearch;
using VideoSearch.Database;
using VideoSearch.Database.Abstract;
using VideoSearch.Indexer;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Translator;
using VideoSearch.VideoDescriber;
using VideoSearch.VideoTranscriber;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// basic infrastructure
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// application
builder.Services.AddVideoDescriber(Environment.GetEnvironmentVariable("VIDEO_DESCRIBER_URL"));
builder.Services.AddVideoTranscriber(Environment.GetEnvironmentVariable("VIDEO_TRANSCRIBER_URL"));
builder.Services.AddTranslator(Environment.GetEnvironmentVariable("LIBRE_TRANSLATE_URL"));

builder.Services.AddDatabase();
builder.Services.AddHostedService<IndexerService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddSingleton<IHintService, HintService>();

// build
WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.Services.GetRequiredService<IStorage>().Init();
await app.Services.GetRequiredService<IHintService>().Rebuild();

app.MapControllers();
app.Run();