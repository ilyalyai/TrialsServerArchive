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
            ViewBag.SampleTypes = _context.SampleTypes.ToList();
            int pageSize = 20; // Количество элементов на странице
            int pageNumber = page ?? 1; // Номер текущей страницы (по умолчанию 1)

            var entries = _context.Objects.OfType<Sample>().ToPagedList(pageNumber, pageSize);

            return View(entries);
        }

        [HttpPost]
        public IActionResult CreateSample(Sample sample, string SampleTypeId)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Приведение дат к UTC
                    sample.SampleCreationDate = DateTime.SpecifyKind(sample.SampleCreationDate, DateTimeKind.Utc);
                    sample.RecordDate = DateTime.SpecifyKind(sample.RecordDate, DateTimeKind.Utc);
                    // Получение текущего пользователя
                    var user = Task.Run(async () => await _userManager.GetUserAsync(User)).Result;
                    if (user != null && !string.IsNullOrEmpty(user.FullName))
                        sample.CreatedBy = user.GetInitials(); // Используем метод расширения
                    else
                        sample.CreatedBy = "System"; // Резервное значение

                    sample.SampleType = _context.SampleTypes.First(s => s.Id == int.Parse(SampleTypeId));

                    switch (sample.SampleType.Name.ToLower()[0])
                    {
                        case 'ц':
                            //цилинд - пи (д/2) h
                            sample.Density = sample.Weight / (Math.PI * sample.DimensionA * Math.Pow((0.5 * sample.DimensionB), 2));
                            break;

                        //куб, призма и прочее - просто перемножим стороны
                        case 'п':
                        case 'к':
                        default:
                            sample.Density = sample.Weight / (sample.DimensionA * sample.DimensionB * sample.DimensionC ?? 1);
                            break;
                    }

                    _context.Objects.Add(sample);
                    _context.SaveChanges();
                    return Ok(new { Result = true });
                }
                // Сбор ошибок валидации
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    Message = "Ошибки валидации",
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
                return StatusCode(500, new
                {
                    Message = "Ошибка сохранения образца",
                    Error = ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult GroupSamples(string ids, string seriesName)
        {
            var idArray = ids.Split(',').Select(int.Parse).ToArray();
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
                    Id = id,
                    // Копируем все свойства из исходного образца
                    SeriesName = sample.SeriesName,
                    Name = sample.Name,
                    SampleCreationDate = sample.SampleCreationDate,
                    CreatedBy = sample.CreatedBy,

                    // Критически важные свойства
                    SampleId = sample.Id,
                    SampleTypeId = sample.SampleTypeId,

                    // Свойства испытаний
                    TestingDate = testingDate,

                    // Копируем геометрические параметры
                    DimensionA = sample.DimensionA,
                    DimensionB = sample.DimensionB,
                    DimensionC = sample.DimensionC,
                    Weight = sample.Weight,
                    Density = sample.Density,
                    // Дополнительные свойства
                    JournalType = sample.JournalType,
                    Comment = sample.Comment
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
                // Удаление связанных фотографий
                foreach (var photo in sample.Photos)
                {
                    // Здесь должна быть логика удаления файлов
                }
                _context.Objects.Remove(sample);
                _context.SaveChanges();
            }
            return Ok();
        }

        public IActionResult Details(int id)
        {
            ViewBag.Toolings = _context.Toolings.ToList();
            ViewBag.SampleTypes = _context.SampleTypes.ToList();
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

            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult CreateSampleType(SampleType model)
        {
            if (ModelState.IsValid)
            {
                _context.SampleTypes.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromForm] BaseObject model)
        {
            try
            {
                // Найти существующий объект
                var existing = await _context.Objects
                    .Include(o => o.Photos)
                    .FirstOrDefaultAsync(o => o.Id == model.Id);

                if (existing == null)
                {
                    return Json(new { success = false, message = "Образец не найден" });
                }

                // Обновляем свойства
                existing.Name = model.Name;
                existing.SeriesName = model.SeriesName;
                existing.SampleTypeId = model.SampleTypeId;
                existing.SampleCreationDate = model.SampleCreationDate;
                existing.RecordDate = model.RecordDate;
                existing.Weight = model.Weight;
                existing.DimensionA = model.DimensionA;
                existing.DimensionB = model.DimensionB;
                existing.DimensionC = model.DimensionC;
                existing.JournalType = model.JournalType;
                existing.Comment = model.Comment;

                // Сохранить изменения
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(int sampleId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            var sample = await _context.Objects.FindAsync(sampleId);
            if (sample == null) return NotFound("Образец не найден");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var photo = new SamplePhoto
            {
                FileName = $"{sample.Name}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}",
                ContentType = file.ContentType,
                Content = memoryStream.ToArray(),
                SampleId = sampleId
            };

            _context.SamplePhotos.Add(photo);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var photo = await _context.SamplePhotos.FindAsync(id);
            if (photo == null) return NotFound();

            _context.SamplePhotos.Remove(photo);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public IActionResult GetPhoto(int id)
        {
            var photo = _context.SamplePhotos.Find(id);
            if (photo == null) return NotFound();

            return File(photo.Content, photo.ContentType);
        }

        [HttpGet]
        public IActionResult GetPhotos(int sampleId)
        {
            var photos = _context.SamplePhotos
                .Where(p => p.SampleId == sampleId)
                .ToList();

            return PartialView("_SamplePhotos", photos);
        }

        [HttpGet]
        public IActionResult CreateForm()
        {
            try
            {
                // Убедитесь, что передаете SampleTypes во ViewBag
                ViewBag.SampleTypes = _context.SampleTypes.ToList();
                return PartialView("_CreateSamplePartial", new Sample());
            }
            catch (Exception ex)
            {
                // Возвращаем сообщение об ошибке, если что-то пошло не так
                return Content($"<div class='alert alert-danger'>Ошибка загрузки формы: {ex.Message}</div>");
            }
        }
    }
}