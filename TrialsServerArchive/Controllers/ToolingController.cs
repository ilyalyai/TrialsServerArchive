using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models;
using TrialsServerArchive.Models.Objects;
using X.PagedList.Extensions;

namespace TrialsServerArchive.Controllers
{
    [Authorize]
    public class ToolingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ToolingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(int? page)
        {
            int pageSize = 20; // Количество элементов на странице
            int pageNumber = page ?? 1; // Номер текущей страницы (по умолчанию 1)

            var entries = _context.Toolings.ToPagedList(pageNumber, pageSize);

            return View(entries);
        }

        [HttpPost]
        public IActionResult Create(Tooling tooling)
        {
            try
            {
                // Устанавливаем значения перед валидацией
                // Получение текущего пользователя
                var user = Task.Run(async () => await _userManager.GetUserAsync(User)).Result;
                if (user != null && !string.IsNullOrEmpty(user.FullName))
                    tooling.CreatedBy = user.GetInitials(); // Используем метод расширения
                else
                    tooling.CreatedBy = "System"; // Резервное значение
                tooling.ReconciliationDate = DateTime.UtcNow;

                // Удаляем ошибки валидации для этих полей
                ModelState.Remove(nameof(Tooling.CreatedBy));
                ModelState.Remove(nameof(Tooling.ReconciliationDate));

                if (ModelState.IsValid)
                {
                    _context.Toolings.Add(tooling);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }

                // Логирование ошибок валидации
                foreach (var entry in ModelState)
                {
                    if (entry.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"Ошибка в поле '{entry.Key}':");
                        foreach (var error in entry.Value.Errors)
                        {
                            Console.WriteLine($"- {error.ErrorMessage}");
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}