/*
    1. ������ ��������� � �� ��� Sample. 
����� ����� �������� �������� � ���� � ����� ��� ������� ����� (������� ������ "�������� � ����� �"
    2. ����� �� ������ ������������ ��������� ������ ��������� �������. ��� ����� ���������� TrialObject. � ��� ���������� ���������� ���������, �� ��� ���������� ����� �������������
    3. ����� ����� ������ ��������� � ������ (������ "������� � ������"), ������ ���������� ObjectInJournal � �������� ������������ � ������

    � ��� ����� ���� tooling - ��� ��������, ������� ������������ � ����������

� ����� ����� ���������:

1. ����������� ��������� �������. �����, ���� ������� �� ����� ���������
2. �� ������� �������� (� ��������� � ��������) �� ����� ���������� ����� � ������������� (�������� ������� ������� �� �������� - ������ �������)
3. �� ������� ������������� �������� �� �� ������� � ������
4. �� �������-������� �� ����� 
�) ����������� �� ����� ���� ������ �����
�) ����������� ������ � ������� �������
�) �������� ���������

����� 3 �������, 3 ����� (�������� � 2 �������� ����� ���������), �����
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Data;

var builder = WebApplication.CreateBuilder(args);

// ��������� �������� ���� ������
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ��������� Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // ������� ��� ���������� � ������Напр
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 0; // ��������� ������ ������
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    // ��������� ������������ ��� ������������ ������ email
    options.User.AllowedUserNameCharacters = null; // ��������� ����� �������
})
.AddEntityFrameworkStores<AppDbContext>();

// ������������ ����� ��������������
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();  // ����� ��� Identity

var app = builder.Build();

// ��������� ��������� ��������
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

