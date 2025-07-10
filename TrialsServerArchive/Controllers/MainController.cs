using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Models.Objects;

namespace TrialsServerArchive.Controllers
{
    public class MainControllerService : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IWebHostEnvironment _env;
        protected readonly ILogger<TrialsController> _logger;

        public MainControllerService(IWebHostEnvironment env,
                           ILogger<TrialsController> logger,
                           ApplicationDbContext context)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public IActionResult Details(int id)
        {
            var trial = _context.Objects.OfType<ObjectInJournal>()
                .Include(t => t.FurnaceProgram)
                .Include(t => t.StoragePlace)
                .FirstOrDefault(t => t.Id == id);

            if (trial == null) return NotFound();

            ViewBag.FurnacePrograms = _context.FurnacePrograms.ToList();
            ViewBag.StoragePlaces = _context.StoragePlaces.ToList();

            return View(trial);
        }

        public IActionResult GenerateProtocol(int id)
        {
            // Найдем испытание в базе данных
            var trial = _context.Objects.OfType<ObjectInJournal>()
                .Include(t => t.FurnaceProgram)
                .Include(t => t.StoragePlace)
                .FirstOrDefault(t => t.Id == id);
            if (trial == null)
            {
                return NotFound($"Образец с ID {id} не найден");
            }

            // 2. Определение пути к шаблону
            string templatePath = Path.Combine(_env.WebRootPath, "templates", "ProtocolTemplate.docx");

            if (!System.IO.File.Exists(templatePath))
            {
                return NotFound("Шаблон протокола не найден");
            }

            try
            {
                // 3. Чтение шаблона в MemoryStream
                byte[] templateBytes;
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    templateBytes = new byte[fileStream.Length];
                    fileStream.Read(templateBytes, 0, (int)fileStream.Length);
                }

                // 4. Редактирование документа в памяти
                using (var memoryStream = new MemoryStream())
                {
                    // Запись байтов в поток
                    memoryStream.Write(templateBytes, 0, templateBytes.Length);
                    memoryStream.Position = 0;

                    // Редактирование документа
                    using (WordprocessingDocument doc = WordprocessingDocument.Open(memoryStream, true))
                    {
                        MainDocumentPart mainPart = doc.MainDocumentPart;
                        if (mainPart == null) return BadRequest("Неверный формат шаблона");

                        // Получение текста документа
                        string docText;
                        using (StreamReader sr = new StreamReader(mainPart.GetStream()))
                        {
                            docText = sr.ReadToEnd();
                        }

                        // Замена плейсхолдеров
                        docText = docText
                            .Replace("[TrialId]", trial.Id.ToString())
                            .Replace("[TestingDate]", trial.TestingDate.ToString("dd.MM.yyyy"))
                            .Replace("[CurrentDateTime]", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"))
                            .Replace("[SampleName]", trial.Name)
                            .Replace("[TestMode]", trial.TestMode);

                        // Запись измененного текста
                        using (StreamWriter sw = new StreamWriter(mainPart.GetStream(FileMode.Create)))
                        {
                            sw.Write(docText);
                        }
                    }
                    // 5. Подготовка файла для скачивания
                    memoryStream.Position = 0;
                    byte[] fileBytes = memoryStream.ToArray();
                    string fileName = $"Протокол_испытания_{trial.Id}_{DateTime.Now:yyyyMMddHHmmss}.docx";

                    return File(fileBytes,
                              "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                              fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации протокола");
                return StatusCode(500, "Ошибка при генерации протокола");
            }
        }
    }
}
