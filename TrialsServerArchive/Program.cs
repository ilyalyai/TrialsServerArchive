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

// ������������ ��������
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => // �������� �����
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

// ������������ ��������� ��������
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

// ���������� ������ ������� middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();


