using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;

[Authorize]
public class SamplesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SamplesController(ApplicationDbContext context) => _context = context;

    public IActionResult Index()
    {
        ViewBag.Toolings = _context.Toolings.ToList();
        return View(_context.Objects.OfType<Sample>().ToList());
    }

    [HttpPost]
    public IActionResult CreateSample(Sample sample)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Приведение дат к UTC
                sample.SampleCreationDate = DateTime.SpecifyKind(sample.SampleCreationDate, DateTimeKind.Utc);
                sample.ManufactureDate = DateTime.SpecifyKind(sample.ManufactureDate, DateTimeKind.Utc);

                _context.Objects.Add(sample);
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
            return View(sample);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            return View(sample);
        }
    }

    [HttpPost]
    public IActionResult GroupSamples(int[] ids, int seriesId, string seriesName)
    {
        var samples = _context.Objects.OfType<Sample>().Where(s => ids.Contains(s.Id));
        foreach (var sample in samples)
        {
            sample.SeriesId = seriesId;
            sample.SeriesName = seriesName;
        }
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult MoveToTrials(int[] ids, int[] toolingIds, DateTime testingDate)
    {
        foreach (var id in ids)
        {
            var sample = _context.Objects.OfType<Sample>().First(s => s.Id == id);
            var trial = new TrialObject
            {
                SeriesId = sample.SeriesId,
                SeriesName = sample.SeriesName,
                Name = sample.Name,
                SampleCreationDate = sample.SampleCreationDate,
                SampleId = sample.Id,
                TestingDate = testingDate
            };

            // Добавляем выбранные инструменты через промежуточную сущность
            if (toolingIds != null)
            {
                foreach (var toolingId in toolingIds)
                {
                    trial.ToolingLinks.Add(new TrialTooling
                    {
                        ToolingId = toolingId
                    });
                }
            }

            _context.Objects.Remove(sample);
            _context.Objects.Add(trial);
        }
        _context.SaveChanges();
        return RedirectToAction("Index", "Trials");
    }
}