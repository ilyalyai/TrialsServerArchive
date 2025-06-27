/*
    1. Объект заносится в бд как Sample. 
Можно сразу добавить объектов к нему в серию или указать позже (добавлю кнопку "добавить в серию к"
    2. Далее по указке пользователя указываем данные испытаний образца. Вся серия становится TrialObject. У них появляются результаты испытаний, но эти результаты можно редактировать
    3. После этого данные заносятся в журнал (кнопка "занести в журнал"), объект становится ObjectInJournal и навсегда записывается в журнал

    У нас также есть tooling - это оснастка, которая используется в испытаниях

В итоге имеем следующее:

1. Возможность добавлять образцы. Форма, плюс таблица со всеми образцами
2. Из таблицы образцов (с фильтрами и плюшками) мы можем переводить серии в испытательные (добавить быстрые фильтры по возрасту - кратно неделям)
3. Из таблицы испытательных бубликов мы их заносим в журнал
4. Из таблицы-журнала мы можем 
а) Фильтровать по всему чему только можно
б) Формировать отчеты и сводные таблицы
в) Протокол испытаний

Итого 3 таблицы, 3 формы (создания и 2 перевода между статусами), логин

Осталось:
1. Представление для журнала образцов (новые атрибуты)
2. Представление журналов ОБ и РЕС (новые атрибуты, сбор в зависимости от типа журнала)
3. Генерация протокола испытаний
*/

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Globalization;
using TrialsServerArchive.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:2310");

// Конфигурация сервисов

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //options.UseNpgsql(connectionString));
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 0;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.AllowedUserNameCharacters = null;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Применяем миграции при запуске
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();  // Применяет миграции
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();