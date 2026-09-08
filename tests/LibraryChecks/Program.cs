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
Check("Cannot borrow duplicate title", (db, s) => Reject(() => s.Borrow(1, 1)));
Check("Return creates correct fine and restores stock", (db, s) => { var b = db.Books.Include(b => b.Loans).Single(b => b.Id == 7); var before = b.Available; s.Return(7); Assert(db.Loans.Find(7)!.Fine == 4000); Assert(b.Available == before + 1); });
Check("Duplicate return cannot inflate stock", (db, s) => { s.Return(7); Reject(() => s.Return(7)); Assert(db.Loans.Find(7)!.Fine == 4000); });
Check("Unpaid fine blocks borrowing", (db, s) => { s.Return(7); Reject(() => s.Borrow(1, 6)); });
Check("Renew increases due date and counter", (db, s) => { var l = db.Loans.Find(10)!; var due = l.DueAt; s.Renew(10); Assert(l.DueAt == due.AddDays(7) && l.Renewals == 1); });
Check("Overdue renewal rejected", (db, s) => Reject(() => s.Renew(7)));
Check("Renewal cap enforced", (db, s) => { s.Renew(10); s.Renew(10); Reject(() => s.Renew(10)); });
Check("Reader cannot renew another reader loan", (db, s) => Reject(() => s.Renew(10, 1)));
Check("Cannot reserve available book", (db, s) => Reject(() => s.Reserve(6, 1)));
Check("Duplicate reservation rejected", (db, s) => { s.Reserve(8, 1); Reject(() => s.Reserve(8, 1)); Assert(db.Reservations.Count() == 1); });
Check("Reservation prevents renewal", (db, s) => { var l = db.Loans.Find(8)!; l.DueAt = DateTime.Today.AddDays(2); db.SaveChanges(); s.Reserve(8, 1); Reject(() => s.Renew(8)); });
Check("Reservation FIFO and fulfillment", (db, s) => { s.Reserve(8, 1); s.Reserve(8, 3); s.Return(8); Reject(() => s.Borrow(3, 8)); s.Borrow(1, 8); Assert(db.Reservations.OrderBy(r => r.Id).First().Status == "Đã nhận"); Assert(db.Books.Include(b => b.Loans).Single(b => b.Id == 8).Available == 0); });
Check("Foreign key blocks deletion of borrowed book", (db, s) => { db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;"); try { db.Database.ExecuteSqlRaw("DELETE FROM Books WHERE Id=7"); } catch (SqliteException) { return; } throw new Exception("FK not enforced"); });
Check("Database rejects negative stock", (db, s) => { try { db.Database.ExecuteSqlRaw("UPDATE Books SET Quantity=-1 WHERE Id=1"); } catch (SqliteException) { return; } throw new Exception("Constraint missing"); });
Console.WriteLine($"RESULT: {passed}/20 checks passed");
