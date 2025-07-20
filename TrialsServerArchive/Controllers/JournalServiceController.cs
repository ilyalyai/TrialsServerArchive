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
using Microsoft.AspNetCore.Identity;

namespace TrialsServerArchive.Controllers
{
    public class MainControllerService : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IWebHostEnvironment _env;
        protected readonly ILogger<TrialsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public MainControllerService(IWebHostEnvironment env,
                           ILogger<TrialsController> logger,
                           ApplicationDbContext context, 
                           UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _userManager = userManager;
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
                        var user = Task.Run(async () => await _userManager.GetUserAsync(User)).Result;
                        if (user != null && !string.IsNullOrEmpty(user.FullName))
                        {
                            ReplacePlaceholder(body, "[FIO]", user.GetInitials());
                            ReplacePlaceholder(body, "[Position]", user.Position);
                        }

                        // Находим таблицу для заполнения данными
                        var tables = body.Descendants<Table>().ToArray();
                        var toolingTable = tables.FirstOrDefault();
                        if (toolingTable == null) return BadRequest("В шаблоне не найдена таблица для оборудования!");

                        var objectTable = tables.Count() > 1 ? tables[1] : null;
                        if (objectTable == null) return BadRequest("В шаблоне не найдена таблица для результатов испытаний!");

                        int counter = 0;
                        foreach (var tool in trials.SelectMany(t => t.Toolings).Distinct())
                        {
                            var newRow = new TableRow();
                            // Добавляем ячейки с данными
                            newRow.AppendChild(CreateTableCell((counter++).ToString()));
                            newRow.AppendChild(CreateTableCell(tool.Name));
                            newRow.AppendChild(CreateTableCell(tool.Id.ToString()));
                            newRow.AppendChild(CreateTableCell(tool.Description));
                            newRow.AppendChild(CreateTableCell(tool.ExpiryDate.ToString()));
                            toolingTable.AppendChild(newRow);
                        }

                        // Добавляем строки для каждого испытания
                        foreach (var trial in trials)
                        {
                            var newRow = new TableRow();
                            // Добавляем ячейки с данными
                            newRow.AppendChild(CreateTableCell(trial.Id.ToString()));
                            newRow.AppendChild(CreateTableCell(trial.Weight.ToString()));
                            newRow.AppendChild(CreateTableCell(trial.Density.ToString()));
                            newRow.AppendChild(CreateTableCell(trial.DimensionA.ToString()));
                            newRow.AppendChild(CreateTableCell(trial.DimensionB.ToString()));
                            newRow.AppendChild(CreateTableCell(trial.DimensionC == null ? "-" : trial.DimensionC.ToString()));
                            newRow.AppendChild(CreateTableCell(trial.BreakingLoad.ToString()));
                            newRow.AppendChild(CreateTableCell("Не понял что писать"));
                            newRow.AppendChild(CreateTableCell("Не понял что писать"));
                            newRow.AppendChild(CreateTableCell("Не понял что писать"));
                            objectTable.AppendChild(newRow);
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