using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;

public class TypesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TypesController> _logger;

    public TypesController(
        ApplicationDbContext context,
        ILogger<TypesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Types
    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await _context.SampleTypes.ToListAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке типов образцов");
            TempData["ErrorMessage"] = "Ошибка при загрузке данных. Попробуйте позже.";
            return View(new List<Type>());
        }
    }

    // POST: Types/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] SampleType type)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Проверка уникальности
                if (await _context.SampleTypes.AnyAsync(t => t.Name == type.Name))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Тип с таким названием уже существует",
                        errors = new List<string> { "Тип с таким названием уже существует" }
                    });
                }

                _context.Add(type);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            // Сбор ошибок валидации
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new
            {
                success = false,
                message = "Ошибки валидации",
                errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании типа образца");
            return Json(new
            {
                success = false,
                message = "Произошла ошибка при создании типа",
                errors = new List<string> { ex.Message }
            });
        }
    }

    // GET: Types/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        try
        {
            if (id == null) return NotFound();

            var type = await _context.SampleTypes.FindAsync(id);
            if (type == null) return NotFound();

            return PartialView("Edit", type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке формы редактирования");
            return Content("<div class='alert alert-danger'>Ошибка при загрузке данных</div>");
        }
    }

    // POST: Types/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] SampleType type)
    {
        try
        {
            if (id != type.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // Проверка уникальности
                if (await _context.SampleTypes
                    .AnyAsync(t => t.Name == type.Name && t.Id != id))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Тип с таким названием уже существует",
                        errors = new List<string> { "Тип с таким названием уже существует" }
                    });
                }

                _context.Update(type);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            // Сбор ошибок валидации
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new
            {
                success = false,
                message = "Ошибки валидации",
                errors
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!TypeExists(type.Id))
            {
                return NotFound();
            }
            else
            {
                _logger.LogError(ex, "Ошибка конкурентности при редактировании типа");
                return Json(new
                {
                    success = false,
                    message = "Ошибка при сохранении. Данные были изменены другим пользователем.",
                    errors = new List<string> { "Конфликт изменений" }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при редактировании типа образца");
            return Json(new
            {
                success = false,
                message = "Произошла ошибка при сохранении изменений",
                errors = new List<string> { ex.Message }
            });
        }
    }

    // POST: Types/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var type = await _context.SampleTypes.FindAsync(id);
            if (type == null) return NotFound();

            // Проверка на использование типа
            if (await _context.Objects.AnyAsync(o => o.SampleTypeId == id))
            {
                return Json(new
                {
                    success = false,
                    message = "Невозможно удалить тип, так как он используется в образцах"
                });
            }

            _context.SampleTypes.Remove(type);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении типа образца");
            return Json(new
            {
                success = false,
                message = "Произошла ошибка при удалении типа",
                errors = new List<string> { ex.Message }
            });
        }
    }

    private bool TypeExists(int id)
    {
        return _context.SampleTypes.Any(e => e.Id == id);
    }
}