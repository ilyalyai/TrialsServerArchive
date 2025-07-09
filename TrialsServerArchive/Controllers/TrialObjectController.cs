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
                .Where(t => !(t is ObjectInJournal))
                .Include(t => t.ToolingLinks)
                .ThenInclude(tt => tt.Tooling)
                .Include(t => t.SampleType)
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
            using (var transaction = _context.Database.BeginTransaction())
            {
                foreach (var id in ids.Split(',').Select(int.Parse))
                {
                    var trial = _context.Objects.OfType<TrialObject>()
                        .Where(t => !(t is ObjectInJournal))
                        .Include(t => t.FurnaceProgram)
                        .Include(t => t.StoragePlace)
                        .Include(t => t.SampleType)
                        .FirstOrDefault(t => t.Id == id);

                    var user = Task.Run(async () => await _userManager.GetUserAsync(User)).Result;
                    string userName = user?.GetInitials() ?? "System";

                    // Создание объекта ObjectInJournal из TrialObject
                    var journal = new ObjectInJournal
                    {
                        SampleTypeId = trial.SampleTypeId,
                        SeriesName = trial.SeriesName,
                        Name = trial.Name,
                        SampleCreationDate = trial.SampleCreationDate,
                        SampleId = trial.SampleId,
                        TestingDate = trial.TestingDate,
                        ArchiveDate = DateTime.UtcNow,
                        ArchivedBy = userName,
                        CreatedBy = trial.CreatedBy,
                        ToolingLinks = trial.ToolingLinks, // Перенос существующих ссылок
                        RecordDate = trial.RecordDate,
                        Weight = trial.Weight,
                        DimensionA = trial.DimensionA,
                        DimensionB = trial.DimensionB,
                        DimensionC = trial.DimensionC,
                        Comment = trial.Comment,
                        JournalType = trial.JournalType,
                        Photos = trial.Photos, // Перенос существующих ссылок
                        TestMode = trial.TestMode,
                        WeightAfterTest = trial.WeightAfterTest,
                        DimensionAAfterTest = trial.DimensionAAfterTest,
                        DimensionBAfterTest = trial.DimensionBAfterTest,
                        DimensionCAfterTest = trial.DimensionCAfterTest,
                        DensityAfterTest = trial.DensityAfterTest,
                        BreakingLoad = trial.BreakingLoad,
                        WetCoefficient = trial.WetCoefficient,
                        TestingTemperature = trial.TestingTemperature,
                        TestingHumidity = trial.TestingHumidity,
                        MU = trial.MU,
                        MUStar = trial.MUStar,
                        PP = trial.PP,
                        FurnaceProgramId = trial.FurnaceProgramId,
                        StoragePlaceId = trial.StoragePlaceId,
                        TestedBy = trial.TestedBy,
                        Density = trial.Density,
                        DaysToNextAge = trial.DaysToNextAge
                    };

                    _context.Objects.Remove(trial);
                    _context.SaveChanges();
                    _context.Objects.Add(journal);
                    _context.SaveChanges();
                }
                transaction.Commit();
            }
            return RedirectToAction("Index", "Trials");
        }

        [HttpPost]
        public IActionResult RemoveFromSeries(int id)
        {
            var trial = _context.Objects.OfType<TrialObject>().Where(t => !(t is ObjectInJournal)).FirstOrDefault(t => t.Id == id);
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
                .Where(t => !(t is ObjectInJournal))
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
            var trial = _context.Objects.OfType<TrialObject>().Where(t => !(t is ObjectInJournal)).FirstOrDefault(t => t.Id == id);
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
                .Where(t => !(t is ObjectInJournal))
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
                .Where(t => !(t is ObjectInJournal))
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
                    var existingTrial = _context.Objects.OfType<TrialObject>().Where(t => !(t is ObjectInJournal)).FirstOrDefault(t => t.Id == id);
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
            return _context.Objects.OfType<TrialObject>().Where(t => !(t is ObjectInJournal)).Any(e => e.Id == id);
        }
    }
}