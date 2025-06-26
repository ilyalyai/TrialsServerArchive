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

            var entries = _context.Objects.OfType<BaseObject>().ToPagedList(pageNumber, pageSize);

            return View(entries);
        }

        [HttpPost]
        public IActionResult CreateSample(BaseObject sample)
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

                    _context.Objects.Add(sample);
                    _context.SaveChanges();
                    return Ok();
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
            var samples = _context.Objects.OfType<BaseObject>().Where(s => idArray.Contains(s.Id));
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
                var sample = _context.Objects.OfType<BaseObject>().First(s => s.Id == id);
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
            var sample = _context.Objects.OfType<BaseObject>().FirstOrDefault(s => s.Id == id);
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
            var samples = _context.Objects.OfType<BaseObject>()
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
            var sample = _context.Objects.OfType<BaseObject>().FirstOrDefault(s => s.Id == id);
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
            var sample = _context.Objects.OfType<BaseObject>().FirstOrDefault(s => s.Id == id);
            if (sample == null) return NotFound();
            return PartialView("_SampleDetails", sample);
        }

        [HttpPost]
        public IActionResult UpdateSample(BaseObject updatedSample)
        {
            var existingSample = _context.Objects.OfType<BaseObject>()
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
        public IActionResult Edit(BaseObject model)
        {
            if (ModelState.IsValid)
            {
                var existingSample = _context.Objects
                    .Include(s => s.Photos)
                    .FirstOrDefault(s => s.Id == model.Id);

                if (existingSample == null) return NotFound();

                // Обновляем свойства
                existingSample.SeriesName = model.SeriesName;
                existingSample.Name = model.Name;
                existingSample.SampleTypeId = model.SampleTypeId;
                existingSample.SampleCreationDate = model.SampleCreationDate;
                existingSample.RecordDate = model.RecordDate;
                existingSample.Weight = model.Weight;
                existingSample.DimensionA = model.DimensionA;
                existingSample.DimensionB = model.DimensionB;
                existingSample.DimensionC = model.DimensionC;
                existingSample.JournalType = model.JournalType;
                existingSample.Comment = model.Comment;

                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(int sampleId, IFormFile file, bool confirm = false)
        {
            var sample = _context.Objects
                .Include(s => s.SampleType)
                .FirstOrDefault(s => s.Id == sampleId);

            if (sample == null) return Json(new { success = false, message = "Образец не найден" });

            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Файл не выбран" });

            try
            {
                // Генерация имени файла
                var baseName = $"{sample.Name}_{sample.SampleType.Name}_{sample.Id}_{sample.SampleCreationDate:yyyyMMdd}";
                var fileName = $"{baseName}_{Guid.NewGuid().ToString()[..8]}{Path.GetExtension(file.FileName)}";

                // Если нужно подтверждение - открываем модальное окно
                if (confirm)
                {
                    return Json(new
                    {
                        success = false,
                        requireConfirmation = true,
                        suggestedName = fileName
                    });
                }

                // Чтение содержимого файла в массив байтов
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var content = memoryStream.ToArray();

                    // Сохранение в БД
                    _context.SamplePhotos.Add(new SamplePhoto
                    {
                        FileName = fileName,
                        ContentType = file.ContentType,
                        Content = content,
                        SampleId = sampleId
                    });

                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeletePhoto(int id)
        {
            var photo = _context.SamplePhotos.Find(id);
            if (photo == null) return Json(new { success = false, message = "Фото не найдено" });

            try
            {
                // Удаление из БД
                _context.SamplePhotos.Remove(photo);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        [HttpGet("GetPhoto/{id}")]
        public IActionResult GetPhoto(int id)
        {
            var photo = _context.SamplePhotos.Find(id);
            if (photo == null) return NotFound();

            // Добавляем заголовки для кэширования
            Response.Headers.Append("Cache-Control", "public, max-age=3600");
            Response.Headers.Append("Expires", DateTime.UtcNow.AddHours(1).ToString("R"));

            return File(photo.Content, photo.ContentType);
        }

        [HttpGet]
        public IActionResult CreateForm()
        {
            try
            {
                // Убедитесь, что передаете SampleTypes во ViewBag
                ViewBag.SampleTypes = _context.SampleTypes.ToList();
                return PartialView("_CreateSamplePartial", new BaseObject());
            }
            catch (Exception ex)
            {
                // Возвращаем сообщение об ошибке, если что-то пошло не так
                return Content($"<div class='alert alert-danger'>Ошибка загрузки формы: {ex.Message}</div>");
            }
        }
    }
}