using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;

[Authorize]
public class SamplesController : Controller
{
    private readonly AppDbContext _context;

    public SamplesController(AppDbContext context) => _context = context;

    public IActionResult Index()
    {
        ViewBag.Toolings = _context.Toolings.ToList();
        return View(_context.Objects.OfType<Sample>().ToList());
    }

    [HttpPost]
    public IActionResult CreateSample(Sample sample)
    {
        _context.Objects.Add(sample);
        _context.SaveChanges();
        return RedirectToAction("Index");
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