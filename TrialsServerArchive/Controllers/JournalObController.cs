using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Identity;

namespace TrialsServerArchive.Controllers
{
    public class JournalObController : MainControllerService
    {
        public JournalObController(IWebHostEnvironment env,
                   ILogger<TrialsController> logger,
                   ApplicationDbContext context, 
                           UserManager<ApplicationUser> userManager) : base(env, logger, context, userManager) { }

        public IActionResult Index(int? page)
        {
            int pageSize = 20; // Количество элементов на странице
            int pageNumber = page ?? 1; // Номер текущей страницы (по умолчанию 1)

            var entries = _context.Objects.OfType<ObjectInJournal>()
                .Where(o => o.JournalType == JournalType.OB)
                .Include(j => j.ToolingLinks)
                .ThenInclude(tt => tt.Tooling)
                .ToPagedList(pageNumber, pageSize);

            return View(entries);
        }

                [HttpPost]
        public IActionResult GenerateProtocol([FromForm] string ids)
        {
            return GenerateBulkProtocol(ids);
        }
    }
}