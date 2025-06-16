using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;

[Authorize]
public class ToolingController : Controller
{
    private readonly ApplicationDbContext _context;

    public ToolingController(ApplicationDbContext context) => _context = context;

    public IActionResult Index() => View(_context.Toolings.ToList());

    [HttpPost]
    public IActionResult Create(Tooling tooling)
    {
        try
        {
            tooling.CreatedBy = User.Identity?.Name ?? "System";
            tooling.ReconciliationDate = DateTime.UtcNow;
            ModelState.ClearValidationState(nameof(tooling));
            if (TryValidateModel(tooling, nameof(tooling)))
            {
                _context.Toolings.Add(tooling);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            // Лог ошибок валидации
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    Console.WriteLine($"Ошибка валидации: {error.ErrorMessage}");
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