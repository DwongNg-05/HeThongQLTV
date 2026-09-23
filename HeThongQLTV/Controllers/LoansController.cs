using System.Security.Claims;
using HeThongQLTV.Data;
using HeThongQLTV.Models;
using HeThongQLTV.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
namespace HeThongQLTV.Controllers;

[Authorize]
public class LoansController(LibraryDb db, CirculationService service) : Controller
{
    private int MemberId => db.Users.Find(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))?.MemberId ?? -1;
    public IActionResult Index(string? q, string? status) { var query = db.Loans.Include(l => l.Book).Include(l => l.Member).AsQueryable(); if (User.IsInRole("DocGia")) query = query.Where(l => l.MemberId == MemberId); var list = query.OrderByDescending(l => l.Id).ToList(); if (!string.IsNullOrWhiteSpace(q)) list = list.Where(l => $"{l.Book.Title} {l.Member.FullName} PM{l.Id:0000}".Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList(); if (!string.IsNullOrEmpty(status)) list = list.Where(l => l.Status == status).ToList(); return View(list); }
    [HttpGet, Authorize(Roles = "Admin,ThuThu")]
    public IActionResult Create()
    {
        Load();
        return View(new LoanCreateInput());
    }

    [HttpPost, Authorize(Roles = "Admin,ThuThu")]
    public IActionResult Create(LoanCreateInput input)
    {
        input.Items ??= [];
        if (input.Items.Sum(i => (long)i.Quantity) > 5)
            ModelState.AddModelError("", "Mỗi lần mượn không được vượt quá 5 quyển sách.");
        if (ModelState.IsValid)
        {
            try
            {
                var count = service.Borrow(input.MemberId, input.Items);
                TempData["Success"] = $"Lập phiếu mượn thành công cho {count} quyển sách. Số bản còn sẵn đã được cập nhật.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        Load();
        input.Items ??= [];
        return View(input);
    }

    private List<Book> Load()
    {
        ViewBag.Members = db.Members.AsNoTracking().Include(m => m.Loans).OrderBy(m => m.FullName).ToList();
        var books = db.Books.AsNoTracking().Include(b => b.Loans).OrderBy(b => b.Title).ToList();
        ViewBag.Books = books;
        return books;
    }
    [HttpPost, Authorize(Roles = "Admin,ThuThu")] public IActionResult Return(int id) => Run(() => service.Return(id), "Đã ghi nhận trả sách và tính tiền phạt nếu quá hạn.");
    [HttpPost] public IActionResult Renew(int id) => Run(() => service.Renew(id, User.IsInRole("DocGia") ? MemberId : null), "Gia hạn thành công.");
    [HttpPost, Authorize(Roles = "Admin,ThuThu")] public IActionResult Pay(int id) => Run(() => { var l = db.Loans.Find(id) ?? throw new InvalidOperationException("Không tìm thấy phiếu."); if (l.Fine <= 0 || l.FinePaid) throw new InvalidOperationException("Không có khoản phạt cần thanh toán."); l.FinePaid = true; db.SaveChanges(); }, "Đã ghi nhận thanh toán tiền phạt.");
    [HttpPost, Authorize(Roles = "Admin,ThuThu")]
    public IActionResult Delete(int id)
    {
        if (!db.Loans.Any(l => l.Id == id)) return NotFound();
        return Run(() => service.DeleteReturnedLoan(id), "Đã xóa phiếu mượn đã trả.");
    }
    private IActionResult Run(Action action, string message) { try { action(); TempData["Success"] = message; } catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; } return RedirectToAction("Index"); }
}
