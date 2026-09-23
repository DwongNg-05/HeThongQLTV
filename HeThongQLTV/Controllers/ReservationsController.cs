using System.Security.Claims;
using HeThongQLTV.Data;
using HeThongQLTV.Models;
using HeThongQLTV.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
namespace HeThongQLTV.Controllers;

[Authorize]
public class ReservationsController(LibraryDb db, CirculationService service) : Controller
{
    private int MemberId => db.Users.Find(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))?.MemberId ?? -1;
    public IActionResult Index() { var q = db.Reservations.Include(r => r.Book).ThenInclude(b => b.Loans).Include(r => r.Member).AsQueryable(); if (User.IsInRole("DocGia")) q = q.Where(r => r.MemberId == MemberId); return View(q.OrderBy(r => r.CreatedAt).ToList()); }
    [HttpGet, Authorize(Roles = "DocGia")]
    public IActionResult Create(int? bookId = null)
    {
        ViewBag.SelectedBookId = bookId;
        return ReservationForm(new ReservationCreateInput());
    }

    [HttpPost, Authorize(Roles = "DocGia")]
    public IActionResult Create(ReservationCreateInput input)
    {
        input.Items ??= [];
        if (ModelState.IsValid)
        {
            try
            {
                var count = service.Reserve(MemberId, input.Items);
                TempData["Success"] = $"Đặt trước sách online thành công cho {count} quyển sách. Nhận sách tại quầy theo thứ tự đăng ký.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return ReservationForm(input);
    }

    private IActionResult ReservationForm(ReservationCreateInput input)
    {
        ViewBag.Books = db.Books.AsNoTracking().Include(b => b.Loans).OrderBy(b => b.Title).ToList();
        ViewBag.PendingCount = db.Reservations.Count(r => r.MemberId == MemberId && r.Status == "Đang chờ");
        return View("Create", input);
    }

    [HttpPost, Authorize(Roles = "Admin,ThuThu")]
    public IActionResult Confirm(int id)
    {
        if (!db.Reservations.Any(r => r.Id == id)) return NotFound();
        try
        {
            var count = service.ConfirmReservation(id);
            TempData["Success"] = $"Đã xác nhận giao {count} quyển sách, lập phiếu mượn và chuyển yêu cầu sang Đã nhận.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }

    [HttpPost, Authorize(Roles = "Admin,ThuThu")]
    public IActionResult Delete(int id)
    {
        if (!db.Reservations.Any(r => r.Id == id)) return NotFound();
        try
        {
            var count = service.DeleteClosedReservation(id);
            TempData["Success"] = $"Đã xóa lịch sử đặt trước {count} quyển sách.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Cancel(int id)
    {
        using var tx = db.Database.BeginTransaction();
        var r = db.Reservations.Find(id);
        if (r == null) return NotFound();
        if (User.IsInRole("DocGia") && r.MemberId != MemberId) return Forbid();
        if (r.Status == "Đang chờ")
        {
            var copies = db.Reservations.Where(x => x.BookId == r.BookId && x.MemberId == r.MemberId
                && x.CreatedAt == r.CreatedAt && x.Status == "Đang chờ").ToList();
            foreach (var copy in copies) copy.Status = "Đã hủy";
            db.SaveChanges(); tx.Commit(); TempData["Success"] = $"Đã hủy yêu cầu đặt trước {copies.Count} quyển sách.";
        }
        if (r.Status != "Đã hủy") TempData["Error"] = "Yêu cầu đã được xử lý, không thể hủy.";
        return RedirectToAction("Index");
    }
}
