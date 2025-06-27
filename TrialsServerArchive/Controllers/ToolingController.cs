using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models;
using TrialsServerArchive.Models.Objects;
using X.PagedList;
using X.PagedList.Extensions;

namespace TrialsServerArchive.Controllers
{
    [Authorize]
    public class ToolingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ToolingController> _logger;

        public ToolingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ToolingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index(int? page)
        {
            int pageSize = 20;
            int pageNumber = page ?? 1;
            var entries = _context.Toolings.ToPagedList(pageNumber, pageSize);
            return View(entries);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Tooling tooling)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                tooling.CreatedBy = user?.GetInitials() ?? "System";

                ModelState.Remove(nameof(Tooling.CreatedBy));
                ModelState.Remove(nameof(Tooling.Id));

                if (ModelState.IsValid)
                {
                    _context.Toolings.Add(tooling);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

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
                _logger.LogError(ex, "Ошибка при создании оснастки");
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    errors = new List<string> { ex.Message }
                });
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tooling = await _context.Toolings.FindAsync(id);
            if (tooling == null)
            {
                return Content("<div class='alert alert-danger'>Оснастка не найдена</div>");
            }

            return PartialView("_EditToolingPartial", tooling);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Tooling tooling)
        {
            try
            {
                if (id != tooling.Id)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Несоответствие идентификаторов"
                    });
                }

                var existingTooling = await _context.Toolings.FindAsync(id);
                if (existingTooling == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Оснастка не найдена"
                    });
                }

                // Обновляем только разрешенные поля
                existingTooling.Name = tooling.Name;
                existingTooling.Description = tooling.Description;
                existingTooling.ReconciliationDate = tooling.ReconciliationDate;
                existingTooling.ExpiryDate = tooling.ExpiryDate;
                existingTooling.VerifiedBy = tooling.VerifiedBy;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при редактировании оснастки");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var tooling = await _context.Toolings.FindAsync(id);
                if (tooling == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Оснастка не найдена"
                    });
                }

                _context.Toolings.Remove(tooling);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении оснастки");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}