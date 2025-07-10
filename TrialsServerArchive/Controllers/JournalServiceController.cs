using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Models.Objects;
using DocumentFormat.OpenXml;

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

        [HttpPost]
        public IActionResult GenerateBulkProtocol([FromForm] string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return BadRequest("Не выбраны испытания");
            }

            try
            {
                var idList = ids.Split(',').Select(int.Parse).ToList();
                var trials = _context.Objects.OfType<ObjectInJournal>()
                    .Where(t => idList.Contains(t.Id))
                    .OrderBy(t => t.SeriesName)
                    .ThenBy(t => t.Id)
                    .ToList();

                if (trials.Count == 0)
                {
                    return NotFound("Испытания не найдены");
                }

                // Путь к шаблону
                string templatePath = Path.Combine(_env.WebRootPath, "templates", "ProtocolTemplate.docx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return NotFound("Шаблон протокола не найден");
                }

                using (var resultStream = new MemoryStream())
                {
                    // Копируем шаблон в результирующий поток
                    using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(resultStream);
                    }

                    // Редактирование документа
                    using (WordprocessingDocument doc = WordprocessingDocument.Open(resultStream, true))
                    {
                        MainDocumentPart mainPart = doc.MainDocumentPart;
                        if (mainPart == null) return BadRequest("Неверный формат шаблона");

                        Body body = mainPart.Document.Body;

                        // Заменяем общие плейсхолдеры
                        ReplacePlaceholder(body, "[CurrentDate]", DateTime.Now.ToString("dd.MM.yyyy"));
                        ReplacePlaceholder(body, "[TotalCount]", trials.Count.ToString());

                        // Находим таблицу для заполнения данными
                        var table = body.Descendants<Table>().FirstOrDefault();
                        if (table == null) return BadRequest("В шаблоне не найдена таблица");

                        // Добавляем строки для каждого испытания
                        foreach (var trial in trials)
                        {
                            var newRow = new TableRow();

                            // Добавляем ячейки с данными
                            newRow.AppendChild(CreateTableCell(trial.Id.ToString()));
                            newRow.AppendChild(CreateTableCell(trial.SampleCreationDate.ToString("dd.MM.yyyy")));
                            newRow.AppendChild(CreateTableCell(trial.Name));
                            newRow.AppendChild(CreateTableCell(trial.TestingDate.ToString("dd.MM.yyyy")));
                            newRow.AppendChild(CreateTableCell(trial.TestMode));
                            table.AppendChild(newRow);
                        }

                        doc.Save();
                    }
                    // Подготовка файла для скачивания
                    resultStream.Position = 0;
                    var resultFile = resultStream.ToArray();
                    string fileName = $"Протокол_испытаний_{DateTime.Now:yyyyMMddHHmmss}.docx";

                    return File(resultFile,
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации группового протокола");
                return StatusCode(500, "Ошибка при генерации протокола");
            }
        }

        // Вспомогательный метод для замены плейсхолдера
        private void ReplacePlaceholder(OpenXmlElement parent, string placeholder, string value)
        {
            foreach (var text in parent.Descendants<Text>())
            {
                if (text.Text.Contains(placeholder))
                {
                    text.Text = text.Text.Replace(placeholder, value);
                }
            }
        }

        // Вспомогательный метод для создания ячейки таблицы
        private TableCell CreateTableCell(string content)
        {
            return new TableCell(
                new Paragraph(
                    new Run(
                        new Text(content)
                    )
                ));
        }
    }
}