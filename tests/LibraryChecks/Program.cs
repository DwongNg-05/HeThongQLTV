using HeThongQLTV.Data;
using HeThongQLTV.Models;
using HeThongQLTV.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
var passed = 0;
void Check(string name, Action<LibraryDb, CirculationService> test) { using var connection = new SqliteConnection("Data Source=:memory:"); connection.Open(); using var db = new LibraryDb(new DbContextOptionsBuilder<LibraryDb>().UseSqlite(connection).Options); db.Seed(); var service = new CirculationService(db, Options.Create(new LibraryRules())); test(db, service); Console.WriteLine("PASS " + name); passed++; }
void Assert(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
void Reject(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new Exception("Expected rejection"); }
Check("Seed has 3 roles and hashed passwords", (db, s) => { Assert(db.Users.Count() == 3); var u = db.Users.First(); Assert(u.PasswordHash != "ThuVien@123"); Assert(new PasswordHasher<AppUser>().VerifyHashedPassword(u, u.PasswordHash, "ThuVien@123") != PasswordVerificationResult.Failed); });
Check("Borrow persists and decreases availability", (db, s) => { var b = db.Books.Include(b => b.Loans).Single(b => b.Id == 6); var before = b.Available; s.Borrow(1, 6); Assert(b.Available == before - 1); Assert(db.Loans.OrderBy(l => l.Id).Last().DueAt == DateTime.Today.AddDays(14)); });
Check("Cannot borrow unavailable book", (db, s) => Reject(() => s.Borrow(1, 8)));
Check("Cannot borrow with expired card", (db, s) => { db.Members.Find(1)!.ExpiresAt = DateTime.Today.AddDays(-1); db.SaveChanges(); Reject(() => s.Borrow(1, 6)); });
Check("Cannot borrow with locked card", (db, s) => { db.Members.Find(1)!.Active = false; db.SaveChanges(); Reject(() => s.Borrow(1, 6)); });
Check("Cannot borrow above configured limit", (db, s) => { for (int i = 0; i < 4; i++) db.Loans.Add(new Loan { BookId = 6, MemberId = 1, DueAt = DateTime.Today.AddDays(14) }); db.SaveChanges(); Reject(() => s.Borrow(1, 5)); });
Check("Can borrow another copy of the same title within limits", (db, s) => { var before = db.Loans.Count(l => l.MemberId == 1 && l.BookId == 1 && l.ReturnedAt == null); s.Borrow(1, 1); Assert(db.Loans.Count(l => l.MemberId == 1 && l.BookId == 1 && l.ReturnedAt == null) == before + 1); });
Check("Return creates correct fine and restores stock", (db, s) => { var b = db.Books.Include(b => b.Loans).Single(b => b.Id == 7); var before = b.Available; s.Return(7); Assert(db.Loans.Find(7)!.Fine == 4000); Assert(b.Available == before + 1); });
Check("Duplicate return cannot inflate stock", (db, s) => { s.Return(7); Reject(() => s.Return(7)); Assert(db.Loans.Find(7)!.Fine == 4000); });
Check("Unpaid fine blocks borrowing", (db, s) => { s.Return(7); Reject(() => s.Borrow(1, 6)); });
Check("Renew increases due date and counter", (db, s) => { var l = db.Loans.Find(10)!; var due = l.DueAt; s.Renew(10); Assert(l.DueAt == due.AddDays(7) && l.Renewals == 1); });
Check("Overdue renewal rejected", (db, s) => Reject(() => s.Renew(7)));
Check("Renewal cap enforced", (db, s) => { s.Renew(10); s.Renew(10); Reject(() => s.Renew(10)); });
Check("Reader cannot renew another reader loan", (db, s) => Reject(() => s.Renew(10, 1)));
Check("Available book can be reserved without creating a loan", (db, s) => { var b = db.Books.Include(b => b.Loans).Single(b => b.Id == 6); var available = b.Available; var loans = db.Loans.Count(); s.Reserve(6, 1); db.ChangeTracker.Clear(); var r = db.Reservations.Single(); Assert(r.BookId == 6 && r.MemberId == 1 && r.Status == "Đang chờ"); Assert(db.Loans.Count() == loans); Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available == available); });
Check("Available book reservation rejects duplicates", (db, s) => { s.Reserve(6, 1); Reject(() => s.Reserve(6, 1)); Assert(db.Reservations.Count() == 1); });
Check("Available book reservation preserves FIFO and fulfillment", (db, s) => { s.Reserve(6, 1); s.Reserve(6, 3); Reject(() => s.Borrow(3, 6)); s.Borrow(1, 6); Assert(db.Reservations.OrderBy(r => r.Id).First().Status == "Đã nhận"); s.Borrow(3, 6); Assert(db.Reservations.All(r => r.Status == "Đã nhận")); });
Check("Available book reservation rejects invalid reader", (db, s) => { Reject(() => s.Reserve(6, -1)); Assert(!db.Reservations.Any()); });
Check("Available book reservation rejects locked card", (db, s) => { db.Members.Find(1)!.Active = false; db.SaveChanges(); Reject(() => s.Reserve(6, 1)); Assert(!db.Reservations.Any()); });
Check("Available book reservation rejects expired card", (db, s) => { db.Members.Find(1)!.ExpiresAt = DateTime.Today.AddDays(-1); db.SaveChanges(); Reject(() => s.Reserve(6, 1)); Assert(!db.Reservations.Any()); });
Check("Available book reservation rejects unpaid fine", (db, s) => { s.Return(7); Reject(() => s.Reserve(6, 1)); Assert(!db.Reservations.Any()); });
Check("Duplicate reservation rejected", (db, s) => { s.Reserve(8, 1); Reject(() => s.Reserve(8, 1)); Assert(db.Reservations.Count() == 1); });
Check("Reservation prevents renewal", (db, s) => { var l = db.Loans.Find(8)!; l.DueAt = DateTime.Today.AddDays(2); db.SaveChanges(); s.Reserve(8, 1); Reject(() => s.Renew(8)); });
Check("Reservation FIFO and fulfillment", (db, s) => { s.Reserve(8, 1); s.Reserve(8, 3); s.Return(8); Reject(() => s.Borrow(3, 8)); s.Borrow(1, 8); Assert(db.Reservations.OrderBy(r => r.Id).First().Status == "Đã nhận"); Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 8).Available == 0); });
Check("Foreign key blocks deletion of borrowed book", (db, s) => { db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;"); try { db.Database.ExecuteSqlRaw("DELETE FROM Books WHERE Id=7"); } catch (SqliteException) { return; } throw new Exception("FK not enforced"); });
Check("Database rejects negative stock", (db, s) => { try { db.Database.ExecuteSqlRaw("UPDATE Books SET Quantity=-1 WHERE Id=1"); } catch (SqliteException) { return; } throw new Exception("Constraint missing"); });
Check("Multi-title multi-copy borrowing decreases each stock", (db, s) => {
    var before6 = db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available;
    var before5 = db.Books.Include(b => b.Loans).Single(b => b.Id == 5).Available;
    var count = s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 }, new LoanBookInput { BookId = 5, Quantity = 1 } });
    Assert(count == 3); db.ChangeTracker.Clear();
    Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available == before6 - 2);
    Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 5).Available == before5 - 1);
});
Check("Stock shortage rolls back entire selection and reservation", (db, s) => {
    s.Reserve(6, 1); var before = db.Loans.Count();
    Reject(() => s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 1 }, new LoanBookInput { BookId = 8, Quantity = 1 } }));
    db.SaveChanges(); db.ChangeTracker.Clear();
    Assert(db.Loans.Count() == before); Assert(db.Reservations.Single().Status == "Đang chờ");
});
Check("Cannot request more copies than remaining stock", (db, s) => {
    db.Books.Find(6)!.Quantity = 1; db.SaveChanges(); var before = db.Loans.Count();
    Reject(() => s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } })); Assert(db.Loans.Count() == before);
});
Check("Duplicate input rows cannot bypass stock limit", (db, s) => {
    db.Books.Find(6)!.Quantity = 1; db.SaveChanges();
    Reject(() => s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 1 }, new LoanBookInput { BookId = 6, Quantity = 1 } }));
});
Check("Total copies count toward reader limit", (db, s) => {
    Reject(() => s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 3 }, new LoanBookInput { BookId = 5, Quantity = 1 } }));
});
Check("Zero negative and excessive quantities rejected", (db, s) => {
    foreach (var quantity in new[] { 0, -1, int.MaxValue })
        Reject(() => s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = quantity } }));
    Reject(() => s.Borrow(1, Array.Empty<LoanBookInput>()));
});
Check("Returning one copy restores exactly one stock", (db, s) => {
    var before = db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available;
    s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } });
    var id = db.Loans.OrderByDescending(l => l.Id).First().Id; s.Return(id); db.ChangeTracker.Clear();
    Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available == before - 1);
    Reject(() => s.Return(id));
});
Check("Multi-copy borrowing keeps copies for waiting readers", (db, s) => {
    db.Books.Find(6)!.Quantity = 2; db.SaveChanges(); s.Reserve(6, 1); s.Reserve(6, 3);
    Reject(() => s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } }));
    s.Borrow(1, 6); s.Borrow(3, 6); db.ChangeTracker.Clear();
    Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available == 0);
});
Check("Reserve multiple titles and copies without changing stock", (db, s) => {
    var before = db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available;
    Assert(s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 }, new LoanBookInput { BookId = 8, Quantity = 1 } }) == 3);
    db.ChangeTracker.Clear(); Assert(db.Reservations.Count() == 3);
    Assert(db.Reservations.Count(r => r.BookId == 6) == 2);
    Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available == before);
});
Check("Reservation batch failure leaves no partial request", (db, s) => {
    Reject(() => s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 }, new LoanBookInput { BookId = 99999, Quantity = 1 } }));
    db.SaveChanges(); Assert(!db.Reservations.Any());
});
Check("Reservation total and pending limit enforced", (db, s) => {
    Reject(() => s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 3 }, new LoanBookInput { BookId = 5, Quantity = 3 } }));
    s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 4 } });
    Reject(() => s.Reserve(1, new[] { new LoanBookInput { BookId = 5, Quantity = 2 } }));
    Assert(db.Reservations.Count() == 4);
});
Check("Partial pickup leaves remaining reserved copies pending", (db, s) => {
    s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } });
    s.Reserve(6, 3); s.Borrow(1, 6);
    Assert(db.Reservations.Count(r => r.MemberId == 1 && r.Status == "Đã nhận") == 1);
    Assert(db.Reservations.Count(r => r.MemberId == 1 && r.Status == "Đang chờ") == 1);
    Reject(() => s.Borrow(3, 6)); s.Borrow(1, 6); s.Borrow(3, 6);
    Assert(db.Reservations.All(r => r.Status == "Đã nhận"));
});
Check("Multiple reserved copies can be received together", (db, s) => {
    s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } });
    s.Borrow(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } });
    Assert(db.Reservations.Count(r => r.Status == "Đã nhận") == 2);
});
Check("Cannot delete active reservation", (db, s) => {
    s.Reserve(6, 1); Reject(() => s.DeleteClosedReservation(db.Reservations.Single().Id));
    Assert(db.Reservations.Count() == 1);
});
Check("Delete canceled copies without deleting other reader requests", (db, s) => {
    s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } }); s.Reserve(6, 3);
    var own = db.Reservations.Where(r => r.MemberId == 1).ToList();
    foreach (var r in own) r.Status = "Đã hủy"; db.SaveChanges();
    Assert(s.DeleteClosedReservation(own[0].Id) == 2);
    Assert(db.Reservations.Single().MemberId == 3);
});
Check("Deleting received copies preserves loans and remaining reservations", (db, s) => {
    s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } }); s.Borrow(1, 6);
    var before = db.Loans.Count(); var received = db.Reservations.Single(r => r.Status == "Đã nhận");
    Assert(s.DeleteClosedReservation(received.Id) == 1); Assert(db.Loans.Count() == before);
    Assert(db.Reservations.Single().Status == "Đang chờ");
});
Check("Confirm reservation borrows every copy and decreases stock", (db, s) => {
    var before = db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available;
    var loans = db.Loans.Count(); s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } });
    var id = db.Reservations.First().Id; Assert(s.ConfirmReservation(id) == 2);
    Assert(db.Loans.Count() == loans + 2); Assert(db.Reservations.All(r => r.Status == "Đã nhận"));
    db.ChangeTracker.Clear(); Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 6).Available == before - 2);
    Reject(() => s.ConfirmReservation(id)); Assert(db.Loans.Count() == loans + 2);
});
Check("Confirm unavailable reservation changes nothing", (db, s) => {
    s.Reserve(8, 1); var loans = db.Loans.Count(); Reject(() => s.ConfirmReservation(db.Reservations.Single().Id));
    Assert(db.Loans.Count() == loans); Assert(db.Reservations.Single().Status == "Đang chờ");
});
Check("Confirm insufficient quantity is all or nothing", (db, s) => {
    db.Books.Find(6)!.Quantity = 1; db.SaveChanges();
    s.Reserve(1, new[] { new LoanBookInput { BookId = 6, Quantity = 2 } });
    var loans = db.Loans.Count(); Reject(() => s.ConfirmReservation(db.Reservations.First().Id));
    Assert(db.Loans.Count() == loans); Assert(db.Reservations.All(r => r.Status == "Đang chờ"));
});
Check("Confirm respects FIFO", (db, s) => {
    s.Reserve(6, 1); s.Reserve(6, 3);
    Reject(() => s.ConfirmReservation(db.Reservations.Single(r => r.MemberId == 3).Id));
    s.ConfirmReservation(db.Reservations.Single(r => r.MemberId == 1).Id);
    s.ConfirmReservation(db.Reservations.Single(r => r.MemberId == 3).Id);
    Assert(db.Reservations.All(r => r.Status == "Đã nhận"));
});
Check("Confirm rejects canceled request and invalid card", (db, s) => {
    s.Reserve(6, 1); var r = db.Reservations.Single(); r.Status = "Đã hủy"; db.SaveChanges();
    Reject(() => s.ConfirmReservation(r.Id));
    r.Status = "Đang chờ"; db.Members.Find(1)!.Active = false; db.SaveChanges();
    Reject(() => s.ConfirmReservation(r.Id)); Assert(r.Status == "Đang chờ");
});
Check("Delete returned loan preserves available stock", (db, s) => {
    var bookId = db.Loans.Find(1)!.BookId;
    var before = db.Books.Include(b => b.Loans).Single(b => b.Id == bookId).Available;
    s.DeleteReturnedLoan(1); db.ChangeTracker.Clear(); Assert(db.Loans.Find(1) == null);
    Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == bookId).Available == before);
});
Check("Cannot delete unreturned or overdue loan", (db, s) => {
    Reject(() => s.DeleteReturnedLoan(7)); Reject(() => s.DeleteReturnedLoan(10));
    Assert(db.Loans.Find(7) != null && db.Loans.Find(10) != null);
});
Check("Cannot erase unpaid fine by deleting returned loan", (db, s) => {
    s.Return(7); Reject(() => s.DeleteReturnedLoan(7)); Assert(db.Loans.Find(7)!.Fine > 0);
    db.Loans.Find(7)!.FinePaid = true; db.SaveChanges(); s.DeleteReturnedLoan(7);
    Assert(db.Loans.Find(7) == null);
});
Console.WriteLine($"RESULT: {passed} checks passed");
