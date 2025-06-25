using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;
using TrialsServerArchive.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Identity;

namespace TrialsServerArchive.Controllers
{
    [Authorize]
    public class SamplesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SamplesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(int? page)
        {
            ViewBag.Toolings = _context.Toolings.ToList();

            int pageSize = 20; // Количество элементов на странице
            int pageNumber = page ?? 1; // Номер текущей страницы (по умолчанию 1)

            var entries = _context.Objects.OfType<Sample>().ToPagedList(pageNumber, pageSize);

            return View(entries);
        }

        [HttpPost]
        public IActionResult CreateSample(Sample sample)
        {
            try
            {
                if (sample.ManufactureDate > sample.SampleCreationDate)
                {
                    ModelState.AddModelError("ManufactureDate", "Дата производства не может быть позже даты создания");
                    return RedirectToAction(nameof(Index));
                }
                if (ModelState.IsValid)
                {
                    // Приведение дат к UTC
                    sample.SampleCreationDate = DateTime.SpecifyKind(sample.SampleCreationDate, DateTimeKind.Utc);
                    sample.ManufactureDate = DateTime.SpecifyKind(sample.ManufactureDate, DateTimeKind.Utc);
                    // Получение текущего пользователя
                    var user = Task.Run(async () => await _userManager.GetUserAsync(User)).Result;
                    if (user != null && !string.IsNullOrEmpty(user.FullName))
                        sample.CreatedBy = user.GetInitials(); // Используем метод расширения
                    else
                        sample.CreatedBy = "System"; // Резервное значение

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
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public IActionResult GroupSamples(string ids, string seriesName)
        {
            var idArray = ids.Split(',').Select(int.Parse).ToArray();

            Console.WriteLine($"Группировка: {ids.Length} объектов, серия: {seriesName}");
            var samples = _context.Objects.OfType<Sample>().Where(s => idArray.Contains(s.Id));
            foreach (var sample in samples)
            {
                sample.SeriesName = seriesName;
            }
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult MoveToTrials(string ids, string toolingIds, DateTime testingDate)
        {
            var idArray = ids.Split(',').Select(int.Parse).ToArray();
            var toolIdArray = toolingIds.Split(',').Select(int.Parse).ToArray();
            foreach (var id in idArray)
            {
                var sample = _context.Objects.OfType<Sample>().First(s => s.Id == id);
                var trial = new TrialObject
                {
                    SeriesName = sample.SeriesName,
                    Name = sample.Name,
                    SampleCreationDate = sample.SampleCreationDate,
                    SampleId = sample.Id,
                    TestingDate = testingDate,
                    CreatedBy = sample.CreatedBy
                };

                // Добавляем выбранные инструменты через промежуточную сущность
                if (toolIdArray != null)
                {
                    foreach (var toolingId in toolIdArray)
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

        [HttpPost]
        public IActionResult RemoveFromSeries(int id)
        {
            var sample = _context.Objects.OfType<Sample>().FirstOrDefault(s => s.Id == id);
            if (sample != null)
            {
                sample.SeriesName = null;
                _context.SaveChanges();
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult RemoveSeries(string seriesName)
        {
            var samples = _context.Objects.OfType<Sample>()
                .Where(s => s.SeriesName == seriesName);

            foreach (var sample in samples)
            {
                sample.SeriesName = null;
            }

            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var sample = _context.Objects.OfType<Sample>().FirstOrDefault(s => s.Id == id);
            if (sample != null)
            {
                _context.Objects.Remove(sample);
                _context.SaveChanges();
            }
            return Ok();
        }

        public IActionResult Details(int id)
        {
            var sample = _context.Objects.OfType<Sample>().FirstOrDefault(s => s.Id == id);
            if (sample == null) return NotFound();
            return PartialView("_SampleDetails", sample);
        }

        [HttpPost]
        public IActionResult UpdateSample(Sample updatedSample)
        {
            var existingSample = _context.Objects.OfType<Sample>()
                .FirstOrDefault(s => s.Id == updatedSample.Id);

            if (existingSample == null) return NotFound();

            existingSample.Name = updatedSample.Name;
            existingSample.SeriesName = updatedSample.SeriesName;
            existingSample.SampleCreationDate = updatedSample.SampleCreationDate;
            existingSample.ManufactureDate = updatedSample.ManufactureDate;

            _context.SaveChanges();
            return Ok();
        }
    }
}