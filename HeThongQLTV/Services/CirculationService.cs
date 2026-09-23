using HeThongQLTV.Data;
using HeThongQLTV.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace HeThongQLTV.Services;

public class CirculationService(LibraryDb db, IOptions<LibraryRules> options)
{
    private readonly LibraryRules rules = options.Value;
    public void Borrow(int memberId, int bookId) => Borrow(memberId, [new LoanBookInput { BookId = bookId, Quantity = 1 }]);

    public int Borrow(int memberId, IEnumerable<LoanBookInput> items)
    {
        using var tx = db.Database.BeginTransaction();
        var count = BorrowWithinTransaction(memberId, items);
        tx.Commit();
        return count;
    }

    private int BorrowWithinTransaction(int memberId, IEnumerable<LoanBookInput> items)
    {
        var requested = items?.ToList() ?? [];
        if (requested.Any(i => i == null || i.BookId <= 0 || i.Quantity < 0 || i.Quantity > 10000))
            throw new InvalidOperationException("Số lượng sách mượn không hợp lệ.");
        var selections = requested.Where(i => i.Quantity > 0).GroupBy(i => i.BookId)
            .Select(g => new { BookId = g.Key, Quantity = g.Sum(i => (long)i.Quantity) }).ToList();
        if (selections.Count == 0) throw new InvalidOperationException("Vui lòng chọn ít nhất một quyển sách để mượn.");
        var total = selections.Sum(i => i.Quantity);
        var member = db.Members.AsNoTracking().SingleOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Độc giả không tồn tại.");
        member.Loans = db.Loans.AsNoTracking().Where(l => l.MemberId == memberId).ToList();
        ValidateMember(member);
        if (member.Loans.Count(l => l.ReturnedAt == null) + total > rules.MaxLoans)
            throw new InvalidOperationException($"Tổng số sách đang mượn và mượn thêm không được vượt quá {rules.MaxLoans} quyển.");

        var ids = selections.Select(i => i.BookId).ToList();
        var books = db.Books.AsNoTracking().Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Title, Available = b.Quantity - b.Loans.Count(l => l.ReturnedAt == null) })
            .ToDictionary(b => b.Id);
        var fulfilled = new List<Reservation>();
        foreach (var selection in selections)
        {
            if (!books.TryGetValue(selection.BookId, out var book))
                throw new InvalidOperationException("Sách không tồn tại.");
            if (selection.Quantity > book.Available)
                throw new InvalidOperationException($"Sách «{book.Title}» chỉ còn {book.Available} bản, không đủ cho số lượng yêu cầu {selection.Quantity}.");
            var queue = db.Reservations.Where(r => r.BookId == book.Id && r.Status == "Đang chờ")
                .OrderBy(r => r.CreatedAt).ThenBy(r => r.Id).ToList();
            if (queue.Count > 0 && queue[0].MemberId != memberId)
                throw new InvalidOperationException($"Sách «{book.Title}» đang ưu tiên cho độc giả đứng đầu hàng đặt trước.");
            if (queue.Count > 0)
            {
                var ownWaiting = queue.Count(r => r.MemberId == memberId);
                var reservedForOthers = Math.Min(book.Available - Math.Min(book.Available, ownWaiting), queue.Count(r => r.MemberId != memberId));
                if (selection.Quantity > book.Available - reservedForOthers)
                    throw new InvalidOperationException($"Sách «{book.Title}» cần giữ bản cho các độc giả khác đang đặt trước.");
                fulfilled.AddRange(queue.Where(r => r.MemberId == memberId).Take((int)selection.Quantity));
            }
        }
        // Only modify tracked entities after every requested title passes validation.
        foreach (var reservation in fulfilled) reservation.Status = "Đã nhận";
        foreach (var selection in selections)
            for (var copy = 0; copy < selection.Quantity; copy++)
                db.Loans.Add(new Loan { BookId = selection.BookId, MemberId = memberId, DueAt = DateTime.Today.AddDays(rules.LoanDays) });
        db.SaveChanges();
        return (int)total;
    }
    public void Return(int id)
    {
        using var tx = db.Database.BeginTransaction(); var loan = db.Loans.Find(id) ?? throw new InvalidOperationException("Phiếu mượn không tồn tại.");
        if (loan.ReturnedAt != null) throw new InvalidOperationException("Phiếu này đã được trả trước đó.");
        loan.ReturnedAt = DateTime.Today; loan.Fine = Math.Max(0, (DateTime.Today - loan.DueAt.Date).Days) * (long)rules.FinePerDay; db.SaveChanges(); tx.Commit();
    }
    public void DeleteReturnedLoan(int id)
    {
        using var tx = db.Database.BeginTransaction();
        var loan = db.Loans.Find(id) ?? throw new InvalidOperationException("Không tìm thấy phiếu mượn.");
        if (loan.ReturnedAt == null)
            throw new InvalidOperationException("Chỉ được xóa phiếu mượn đã trả sách.");
        if (loan.Fine > 0 && !loan.FinePaid)
            throw new InvalidOperationException("Vui lòng thu khoản phạt còn nợ trước khi xóa phiếu mượn.");
        db.Loans.Remove(loan);
        db.SaveChanges(); tx.Commit();
    }

    public void Renew(int id, int? memberId = null)
    {
        using var tx = db.Database.BeginTransaction(); var loan = db.Loans.Find(id) ?? throw new InvalidOperationException("Phiếu mượn không tồn tại.");
        if (memberId != null && loan.MemberId != memberId) throw new InvalidOperationException("Bạn không có quyền gia hạn phiếu này.");
        if (loan.ReturnedAt != null || loan.DueAt.Date < DateTime.Today || loan.Renewals >= rules.MaxRenewals) throw new InvalidOperationException("Không thể gia hạn: sách đã trả, quá hạn hoặc hết lượt gia hạn.");
        if (db.Reservations.Any(r => r.BookId == loan.BookId && r.Status == "Đang chờ")) throw new InvalidOperationException("Sách đang có độc giả đặt trước.");
        loan.DueAt = loan.DueAt.AddDays(rules.RenewalDays); loan.Renewals++; db.SaveChanges(); tx.Commit();
    }
    public void Reserve(int bookId, int memberId) => Reserve(memberId, [new LoanBookInput { BookId = bookId, Quantity = 1 }]);

    public int Reserve(int memberId, IEnumerable<LoanBookInput> items)
    {
        var requested = items?.ToList() ?? [];
        if (requested.Count == 0 || requested.Any(i => i == null || i.BookId <= 0 || i.Quantity <= 0 || i.Quantity > 5))
            throw new InvalidOperationException("Vui lòng chọn sách và số lượng nguyên từ 1 đến 5.");
        var selections = requested.GroupBy(i => i.BookId)
            .Select(g => new { BookId = g.Key, Quantity = g.Sum(i => (long)i.Quantity) }).ToList();
        var total = selections.Sum(i => i.Quantity);
        if (total > 5) throw new InvalidOperationException("Tổng số lượng đặt trước không được vượt quá 5 quyển.");
        using var tx = db.Database.BeginTransaction();
        var member = db.Members.Include(m => m.Loans).SingleOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Tài khoản chưa liên kết thẻ độc giả.");
        ValidateMember(member);
        var pending = db.Reservations.Where(r => r.MemberId == memberId && r.Status == "Đang chờ").ToList();
        if (pending.Count + total > 5)
            throw new InvalidOperationException($"Bạn đang có {pending.Count} quyển chờ nhận; tổng số lượng đặt trước đang chờ không được vượt quá 5.");
        var ids = selections.Select(i => i.BookId).ToList();
        if (db.Books.Count(b => ids.Contains(b.Id)) != selections.Count)
            throw new InvalidOperationException("Có sách không còn tồn tại. Vui lòng chọn lại.");
        if (pending.Any(r => ids.Contains(r.BookId)))
            throw new InvalidOperationException("Bạn đã đặt trước sách này. Hãy hủy yêu cầu đang chờ nếu muốn chọn lại số lượng.");
        var createdAt = DateTime.Now;
        foreach (var selection in selections)
            for (var copy = 0; copy < selection.Quantity; copy++)
                db.Reservations.Add(new Reservation { BookId = selection.BookId, MemberId = memberId, CreatedAt = createdAt });
        db.SaveChanges(); tx.Commit();
        return (int)total;
    }
    public int ConfirmReservation(int id)
    {
        using var tx = db.Database.BeginTransaction();
        var reservation = db.Reservations.Find(id) ?? throw new InvalidOperationException("Không tìm thấy yêu cầu đặt trước.");
        if (reservation.Status != "Đang chờ")
            throw new InvalidOperationException("Chỉ được xác nhận yêu cầu đang chờ; yêu cầu này đã được xử lý.");
        var copies = db.Reservations.Where(r => r.BookId == reservation.BookId && r.MemberId == reservation.MemberId
            && r.CreatedAt == reservation.CreatedAt && r.Status == "Đang chờ").OrderBy(r => r.Id).ToList();
        var first = db.Reservations.Where(r => r.BookId == reservation.BookId && r.Status == "Đang chờ")
            .OrderBy(r => r.CreatedAt).ThenBy(r => r.Id).First();
        if (first.Id != copies[0].Id)
            throw new InvalidOperationException("Yêu cầu chưa đến lượt nhận sách. Vui lòng xử lý theo thứ tự đặt trước.");
        var count = BorrowWithinTransaction(reservation.MemberId,
            [new LoanBookInput { BookId = reservation.BookId, Quantity = copies.Count }]);
        tx.Commit();
        return count;
    }

    public int DeleteClosedReservation(int id)
    {
        using var tx = db.Database.BeginTransaction();
        var reservation = db.Reservations.Find(id) ?? throw new InvalidOperationException("Không tìm thấy yêu cầu đặt trước.");
        if (reservation.Status != "Đã hủy" && reservation.Status != "Đã nhận")
            throw new InvalidOperationException("Chỉ được xóa yêu cầu đã hủy hoặc đã nhận.");
        var copies = db.Reservations.Where(r => r.BookId == reservation.BookId && r.MemberId == reservation.MemberId
            && r.CreatedAt == reservation.CreatedAt && r.Status == reservation.Status).ToList();
        db.Reservations.RemoveRange(copies);
        db.SaveChanges(); tx.Commit();
        return copies.Count;
    }

    private static void ValidateMember(Member m)
    {
        if (!m.Active || m.ExpiresAt.Date < DateTime.Today) throw new InvalidOperationException("Thẻ độc giả đã khóa hoặc hết hạn.");
        if (m.Loans.Any(l => l.Fine > 0 && !l.FinePaid)) throw new InvalidOperationException("Độc giả còn khoản phạt chưa thanh toán.");
    }
}
