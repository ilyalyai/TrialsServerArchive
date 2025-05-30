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
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Data;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация сервисов
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => // Изменено здесь
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Конфигурация конвейера запросов
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

// Критически важный порядок middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();


