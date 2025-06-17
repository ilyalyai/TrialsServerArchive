using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace TrialsServerArchive.Controllers
{
    public class JournalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JournalController(ApplicationDbContext context) => _context = context;

        public IActionResult Index(int? page)
        {
            int pageSize = 20; // Количество элементов на странице
            int pageNumber = page ?? 1; // Номер текущей страницы (по умолчанию 1)

            var entries = _context.Objects.OfType<ObjectInJournal>()
                .Include(j => j.ToolingLinks)
                .ThenInclude(tt => tt.Tooling)
                .ToPagedList(pageNumber, pageSize);

            return View(entries);
        }
    }
}