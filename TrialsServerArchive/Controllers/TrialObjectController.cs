using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models;
using TrialsServerArchive.Models.Objects;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.Mvc.Core;

namespace TrialsServerArchive.Controllers
{
    [Authorize]
    public class TrialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly int[] StandardAges = { 2, 7, 9, 28, 90, 180, 360 };

        public TrialsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(int? page)
        {
            int pageSize = 20;
            int pageNumber = page ?? 1;

            var trials = _context.Objects.OfType<TrialObject>()
                .Include(t => t.ToolingLinks)
                .ThenInclude(tt => tt.Tooling)
                .AsEnumerable()
                .Select(trial =>
                {
                    // Вычисляем возраст на момент испытания
                    var testingAge = trial.TestingAge;

                    // Находим следующий нормативный возраст
                    var nextStandardAge = StandardAges.FirstOrDefault(a => a > (testingAge ?? 0));

                    // Вычисляем сколько дней осталось до следующего возраста
                    trial.DaysToNextAge = nextStandardAge - (testingAge ?? 0);
                    return trial;
                })
                .ToList();

            var groupedTrials = trials
                .GroupBy(t => t.SeriesName)
                .OrderBy(g => g.Key)
                .SelectMany(g => g)
                .ToPagedList(pageNumber, pageSize);

            return View(groupedTrials);
        }

        [HttpPost]
        public IActionResult MoveToJournal(string ids)
        {
            foreach (var id in ids.Split(',').Select(int.Parse))
            {
                var trial = _context.Objects.OfType<TrialObject>()
                    .Include(t => t.ToolingLinks)
                    .First(t => t.Id == id);

                var user = Task.Run(async () => await _userManager.GetUserAsync(User)).Result;
                string userName = user?.GetInitials() ?? "System";

                var journal = new ObjectInJournal
                {
                    SeriesName = trial.SeriesName,
                    Name = trial.Name,
                    SampleCreationDate = trial.SampleCreationDate,
                    SampleId = trial.SampleId,
                    TestingDate = trial.TestingDate,
                    ArchiveDate = DateTime.UtcNow,
                    ArchivedBy = userName,
                    CreatedBy = trial.CreatedBy,
                    ToolingLinks = trial.ToolingLinks.Select(tt => new TrialTooling
                    {
                        ToolingId = tt.ToolingId
                    }).ToList()
                };

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
            return RedirectToAction(nameof(Index));
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
            return RedirectToAction(nameof(Index));
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
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(int id)
        {
            var trial = _context.Objects.OfType<TrialObject>()
                .Include(t => t.FurnaceProgram)
                .Include(t => t.StoragePlace)
                .FirstOrDefault(t => t.Id == id);

            if (trial == null) return NotFound();

            ViewBag.FurnacePrograms = _context.FurnacePrograms.ToList();
            ViewBag.StoragePlaces = _context.StoragePlaces.ToList();

            return View(trial);
        }

        public IActionResult Edit(int id)
        {
            var trial = _context.Objects.OfType<TrialObject>()
                .Include(t => t.FurnaceProgram)
                .Include(t => t.StoragePlace)
                .FirstOrDefault(t => t.Id == id);

            if (trial == null) return NotFound();

            ViewBag.FurnacePrograms = _context.FurnacePrograms.ToList();
            ViewBag.StoragePlaces = _context.StoragePlaces.ToList();

            return View(trial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, TrialObject model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTrial = _context.Objects.OfType<TrialObject>().FirstOrDefault(t => t.Id == id);
                    if (existingTrial == null) return NotFound();

                    // Обновляем свойства
                    existingTrial.TestingDate = model.TestingDate;
                    existingTrial.TestMode = model.TestMode;
                    existingTrial.FurnaceProgramId = model.FurnaceProgramId;
                    existingTrial.StoragePlaceId = model.StoragePlaceId;
                    existingTrial.WeightAfterTest = model.WeightAfterTest;
                    existingTrial.DimensionAAfterTest = model.DimensionAAfterTest;
                    existingTrial.DimensionBAfterTest = model.DimensionBAfterTest;
                    existingTrial.DimensionCAfterTest = model.DimensionCAfterTest;
                    existingTrial.Density = model.Density;
                    existingTrial.BreakingLoad = model.BreakingLoad;
                    existingTrial.WetCoefficient = model.WetCoefficient;
                    existingTrial.TestingTemperature = model.TestingTemperature;
                    existingTrial.TestingHumidity = model.TestingHumidity;
                    existingTrial.MU = model.MU;
                    existingTrial.MUStar = model.MUStar;
                    existingTrial.PP = model.PP;
                    existingTrial.TestedBy = model.TestedBy;
                    existingTrial.Comment = model.Comment;

                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrialExists(id)) return NotFound();
                    throw;
                }
            }

            ViewBag.FurnacePrograms = _context.FurnacePrograms.ToList();
            ViewBag.StoragePlaces = _context.StoragePlaces.ToList();
            return View(model);
        }

        private bool TrialExists(int id)
        {
            return _context.Objects.OfType<TrialObject>().Any(e => e.Id == id);
        }
    }
}