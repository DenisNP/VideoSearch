using VideoSearch.Database;
using VideoSearch.Database.Abstract;
using VideoSearch.Indexer;
using VideoSearch.Translator;
using VideoSearch.Vectorizer;
using VideoSearch.VideoDescriber;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// basic infrastructure
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// application
builder.Services.AddVideoDescriber(Environment.GetEnvironmentVariable("VIDEO_DESCRIBER_URL"));
builder.Services.AddTranslator(Environment.GetEnvironmentVariable("LIBRE_TRANSLATE_URL"));
builder.Services.AddVectorizer(Environment.GetEnvironmentVariable("NAVEC_API_URL"));

builder.Services.AddDatabase();
builder.Services.AddHostedService<IndexerService>();
builder.Services.AddSingleton<SearchService>();

// build
WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.Services.GetRequiredService<IStorage>().Init();

app.MapControllers();
app.Run();