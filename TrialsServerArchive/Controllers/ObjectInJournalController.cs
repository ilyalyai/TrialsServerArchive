using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;
using Microsoft.EntityFrameworkCore;

public class JournalController : Controller
{
    private readonly AppDbContext _context;

    public JournalController(AppDbContext context) => _context = context;

    public IActionResult Index()
    {
        var entries = _context.Objects.OfType<ObjectInJournal>()
            .Include(j => j.ToolingLinks)       // Загружаем связи
            .ThenInclude(tt => tt.Tooling)       // Загружаем инструменты
            .ToList();

        return View(entries);
    }
}