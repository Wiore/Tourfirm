using Web.Components;
using Web.Components.Services;

var builder = WebApplication.CreateBuilder(args);

// HttpClient для обращения к API Turfirm.
// Регистрируем как Singleton: HttpClient потокобезопасен и рассчитан на
// долгое переиспользование (создавать новый на каждый запрос — антипаттерн).
// Важно: handler создаётся ВНУТРИ фабрики, а не снаружи. Иначе один и тот же
// handler захватывается замыканием и раздаётся всем scope'ам — при prerender
// Blazor короткий request-scope dispos'ит свой HttpClient вместе с этим
// handler'ом, а интерактивный circuit получает уже disposed SocketsHttpHandler
// → "Cannot access a disposed object".
// В разработке API использует самоподписанный dev-сертификат, поэтому доверяем
// любому сертификату сервера (ТОЛЬКО для разработки!).
builder.Services.AddSingleton(sp =>
{
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    return new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") };
});

// Сессионное состояние пользователя в пределах circuit (Blazor Server).
builder.Services.AddScoped<UserSession>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();