using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class TrialsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TrialsController(ApplicationDbContext context) => _context = context;

    public IActionResult Index()
    {
        var trials = _context.Objects.OfType<TrialObject>()
            .Include(t => t.ToolingLinks)       // Загружаем промежуточные связи
            .ThenInclude(tt => tt.Tooling)       // Затем загружаем связанные инструменты
            .ToList();

        return View(trials);
    }

    [HttpPost]
    public IActionResult MoveToJournal(int[] ids)
    {
        foreach (var id in ids)
        {
            // Загружаем TrialObject вместе со связями
            var trial = _context.Objects.OfType<TrialObject>()
                .Include(t => t.ToolingLinks)   // Важно: загружаем связи
                .First(t => t.Id == id);

            var journal = new ObjectInJournal
            {
                // Копируем свойства
                SeriesId = trial.SeriesId,
                SeriesName = trial.SeriesName,
                Name = trial.Name,
                SampleCreationDate = trial.SampleCreationDate,
                SampleId = trial.SampleId,
                TestingDate = trial.TestingDate,

                // Уникальные свойства
                ArchiveDate = DateTime.UtcNow,
                ArchivedBy = User.Identity.Name
            };

            // Копируем связи с инструментами
            foreach (var link in trial.ToolingLinks)
            {
                journal.ToolingLinks.Add(new TrialTooling
                {
                    ToolingId = link.ToolingId
                });
            }

            _context.Objects.Remove(trial);
            _context.Objects.Add(journal);
        }
        _context.SaveChanges();
        return RedirectToAction("Index", "Journal");
    }
}