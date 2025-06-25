using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models;
using TrialsServerArchive.Models.Objects;
using X.PagedList.Extensions;

namespace TrialsServerArchive.Controllers
{
    [Authorize]
    public class TrialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrialsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(int? page)
        {
            int pageSize = 20; // Количество элементов на странице
            int pageNumber = page ?? 1; // Номер текущей страницы (по умолчанию 1)
            var trials = _context.Objects.OfType<TrialObject>()
                .Include(t => t.ToolingLinks)       // Загружаем промежуточные связи
                .ThenInclude(tt => tt.Tooling)       // Затем загружаем связанные инструменты
                .ToPagedList(pageNumber, pageSize);

            return View(trials);
        }

        [HttpPost]
        public IActionResult MoveToJournal(string ids)
        {
            foreach (var id in ids.Split(',').Select(int.Parse).ToArray())
            {
                // Загружаем TrialObject вместе со связями
                var trial = _context.Objects.OfType<TrialObject>()
                    .Include(t => t.ToolingLinks)   // Важно: загружаем связи
                    .First(t => t.Id == id);
                // Получение текущего пользователя
                var user = Task.Run(async () => await _userManager.GetUserAsync(User)).Result;
                string userName = "System";
                if (user != null && !string.IsNullOrEmpty(user.FullName))
                    userName = user.GetInitials(); // Используем метод расширения
                var journal = new ObjectInJournal
                {
                    // Копируем свойства
                    SeriesName = trial.SeriesName,
                    Name = trial.Name,
                    SampleCreationDate = trial.SampleCreationDate,
                    SampleId = trial.SampleId,
                    TestingDate = trial.TestingDate,

                    // Уникальные свойства
                    ArchiveDate = DateTime.UtcNow,
                    ArchivedBy = userName,

                    CreatedBy = trial.CreatedBy
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

        [HttpPost]
        public IActionResult RemoveFromSeries(int id)
        {
            var trial = _context.Objects.OfType<TrialObject>().FirstOrDefault(t => t.Id == id);
            if (trial != null)
            {
                trial.SeriesName = null;
                _context.SaveChanges();
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult RemoveSeries(string seriesName)
        {
            var trials = _context.Objects.OfType<TrialObject>()
                .Where(t => t.SeriesName == seriesName);

            foreach (var trial in trials)
            {
                trial.SeriesName = null;
            }

            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var trial = _context.Objects.OfType<TrialObject>().FirstOrDefault(t => t.Id == id);
            if (trial != null)
            {
                _context.Objects.Remove(trial);
                _context.SaveChanges();
            }
            return Ok();
        }

        public IActionResult Details(int id)
        {
            var trial = _context.Objects.OfType<TrialObject>()
                .Include(t => t.ToolingLinks)
                .ThenInclude(tt => tt.Tooling)
                .FirstOrDefault(t => t.Id == id);

            if (trial == null) return NotFound();
            return PartialView("_TrialDetails", trial);
        }

        [HttpPost]
        public IActionResult UpdateTrial(TrialObject updatedTrial)
        {
            var existingTrial = _context.Objects.OfType<TrialObject>()
                .FirstOrDefault(t => t.Id == updatedTrial.Id);

            if (existingTrial == null) return NotFound();

            existingTrial.Name = updatedTrial.Name;
            existingTrial.SeriesName = updatedTrial.SeriesName;
            existingTrial.SampleCreationDate = updatedTrial.SampleCreationDate;
            existingTrial.TestingDate = updatedTrial.TestingDate;

            _context.SaveChanges();
            return Ok();
        }
    }
}