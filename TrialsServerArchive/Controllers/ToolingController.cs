using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrialsServerArchive.Data;
using TrialsServerArchive.Models.Objects;

[Authorize]
public class ToolingController : Controller
{
    private readonly AppDbContext _context;

    public ToolingController(AppDbContext context) => _context = context;

    public IActionResult Index() => View(_context.Toolings.ToList());

    [HttpPost]
    public IActionResult Create(Tooling tooling)
    {
        if (ModelState.IsValid)
        {
            tooling.CreatedBy = User.Identity?.Name ?? "System";
            tooling.ReconciliationDate = DateTime.Now;
            _context.Toolings.Add(tooling);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(tooling);
    }
}