// Controllers/OptionsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;

namespace TrialsServerArchive.Controllers
{
    [Authorize]
    public class OptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OptionsController> _logger;

        public OptionsController(ApplicationDbContext context, ILogger<OptionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new OptionsIndexViewModel
            {
                SampleTypes = _context.SampleTypes.ToList(),
                FurnacePrograms = _context.FurnacePrograms.ToList(),
                StoragePlaces = _context.StoragePlaces.ToList()
            };
            return View(model);
        }

        #region SampleType CRUD

        public IActionResult CreateSampleTypeModal()
        {
            return PartialView("_CreateSampleTypeModal", new SampleType());
        }

        [HttpPost]
        public async Task<IActionResult> CreateSampleType(SampleType model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            if (await _context.SampleTypes.AnyAsync(t => t.Name == model.Name))
            {
                return Json(new
                {
                    success = false,
                    errors = new List<string> { "Тип с таким названием уже существует" }
                });
            }

            _context.SampleTypes.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public async Task<IActionResult> EditSampleTypeModal(int id)
        {
            var type = await _context.SampleTypes.FindAsync(id);
            if (type == null) return NotFound();
            return PartialView("_EditSampleTypeModal", type);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSampleType(SampleType model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            if (await _context.SampleTypes.AnyAsync(t =>
                t.Name == model.Name && t.Id != model.Id))
            {
                return Json(new
                {
                    success = false,
                    errors = new List<string> { "Тип с таким названием уже существует" }
                });
            }

            _context.SampleTypes.Update(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSampleType(int id)
        {
            var type = await _context.SampleTypes.FindAsync(id);
            if (type == null) return NotFound();

            if (await _context.Objects.AnyAsync(o => o.SampleTypeId == id))
            {
                return Json(new
                {
                    success = false,
                    error = "Невозможно удалить тип, так как он используется в образцах"
                });
            }

            _context.SampleTypes.Remove(type);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        #endregion

        #region FurnaceProgram CRUD

        public IActionResult CreateFurnaceModal()
        {
            return PartialView("_CreateFurnaceModal", new FurnaceProgram());
        }

        [HttpPost]
        public async Task<IActionResult> CreateFurnace(FurnaceProgram model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            if (await _context.FurnacePrograms.AnyAsync(t => t.Name == model.Name))
            {
                return Json(new
                {
                    success = false,
                    errors = new List<string> { "Программа с таким названием уже существует" }
                });
            }

            _context.FurnacePrograms.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public async Task<IActionResult> EditFurnaceModal(int id)
        {
            var program = await _context.FurnacePrograms.FindAsync(id);
            if (program == null) return NotFound();
            return PartialView("_EditFurnaceModal", program);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFurnace(FurnaceProgram model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            if (await _context.FurnacePrograms.AnyAsync(t =>
                t.Name == model.Name && t.Id != model.Id))
            {
                return Json(new
                {
                    success = false,
                    errors = new List<string> { "Программа с таким названием уже существует" }
                });
            }

            _context.FurnacePrograms.Update(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFurnace(int id)
        {
            var program = await _context.FurnacePrograms.FindAsync(id);
            if (program == null) return NotFound();

            if (_context.Objects.OfType<TrialObject>().Any(o => o.FurnaceProgram.Id == id) || _context.Objects.OfType<ObjectInJournal>().Any(o => o.FurnaceProgram.Id == id))
            {
                return Json(new
                {
                    success = false,
                    error = "Невозможно удалить программу, так как она используется в образцах"
                });
            }

            _context.FurnacePrograms.Remove(program);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        #endregion

        #region StoragePlace CRUD

        public IActionResult CreateStorageModal()
        {
            return PartialView("_CreateStorageModal", new StoragePlace());
        }

        [HttpPost]
        public async Task<IActionResult> CreateStorage(StoragePlace model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            if (await _context.StoragePlaces.AnyAsync(t => t.Name == model.Name))
            {
                return Json(new
                {
                    success = false,
                    errors = new List<string> { "Место хранения с таким названием уже существует" }
                });
            }

            _context.StoragePlaces.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public async Task<IActionResult> EditStorageModal(int id)
        {
            var place = await _context.StoragePlaces.FindAsync(id);
            if (place == null) return NotFound();
            return PartialView("_EditStorageModal", place);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStorage(StoragePlace model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            if (await _context.StoragePlaces.AnyAsync(t =>
                t.Name == model.Name && t.Id != model.Id))
            {
                return Json(new
                {
                    success = false,
                    errors = new List<string> { "Место хранения с таким названием уже существует" }
                });
            }

            _context.StoragePlaces.Update(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStorage(int id)
        {
            var place = await _context.StoragePlaces.FindAsync(id);
            if (place == null) return NotFound();

            if (_context.Objects.OfType<TrialObject>().Any(o => o.StoragePlace.Id == id) || _context.Objects.OfType<ObjectInJournal>().Any(o => o.StoragePlace.Id == id))
            {
                return Json(new
                {
                    success = false,
                    error = "Невозможно удалить место хранения, так как оно используется в образцах"
                });
            }

            _context.StoragePlaces.Remove(place);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        #endregion
    }
}