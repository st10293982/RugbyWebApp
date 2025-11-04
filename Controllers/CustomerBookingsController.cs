using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;
using PassItOnAcademy.Services;
using PassItOnAcademy.ViewModels;

namespace PassItOnAcademy.Controllers
{
    [Authorize]
    [Route("customer-bookings")]
    public class CustomerBookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPayFastService _payfast;
        private readonly PayFastOptions _pfOpt;
        private readonly IPayFastItnVerifier _itn;

        public CustomerBookingsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IPayFastService payfast,
            IOptions<PayFastOptions> pfOpt,
            IPayFastItnVerifier itn)
        {
            _db = db;
            _userManager = userManager;
            _payfast = payfast;
            _pfOpt = pfOpt.Value;
            _itn = itn;
        }

        [HttpGet("create/{id:int}")]
        public async Task<IActionResult> Create(int id)
        {
            var s = await _db.TrainingSessions
                .AsNoTracking()
                .Include(x => x.TrainingProgram)
                .FirstOrDefaultAsync(x => x.Id == id
                                          && x.Status == SessionStatus.Scheduled
                                          && x.StartUtc > DateTime.UtcNow);
            if (s == null) return NotFound();

            var booked = await _db.Bookings.CountAsync(b => b.SessionId == id && b.Status == BookingStatus.Booked);
            if (booked >= s.Capacity) return BadRequest("This session is full.");

            var userId = _userManager.GetUserId(User);
            var already = await _db.Bookings.AnyAsync(b => b.SessionId == id
                                                           && b.UserId == userId
                                                           && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Pending));
            if (already) return RedirectToAction(nameof(Mine));

            var vm = new BookingChooseVM
            {
                SessionId = s.Id,
                Title = s.Title,
                Level = s.Level,
                StartLocal = DateTime.SpecifyKind(s.StartUtc, DateTimeKind.Utc).ToLocalTime(),
                Location = s.Location,
                ProgramName = s.TrainingProgram?.Name,
                Price = s.Price,
                ImageUrl = s.ImageUrl,
                ImageAlt = s.ImageAlt
            };
            return View("Choose", vm);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(int sessionId)
        {
            var nowUtc = DateTime.UtcNow;
            var s = await _db.TrainingSessions
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.Status == SessionStatus.Scheduled);
            if (s == null || s.StartUtc <= nowUtc) return NotFound();

            var booked = await _db.Bookings
                .CountAsync(b => b.SessionId == sessionId && b.Status == BookingStatus.Booked);
            if (booked >= s.Capacity) return BadRequest("This session just sold out.");

            var userId = _userManager.GetUserId(User);
            var dup = await _db.Bookings.AnyAsync(b => b.SessionId == sessionId
                                                       && b.UserId == userId
                                                       && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Pending));
            if (dup) return RedirectToAction(nameof(Mine));

            _db.Bookings.Add(new Booking
            {
                SessionId = sessionId,
                UserId = userId,
                Status = BookingStatus.Booked,
                CreatedUtc = nowUtc
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Booking confirmed.";
            return RedirectToAction(nameof(Mine));
        }

        [HttpPost("cash")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cash(int sessionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var s = await _db.TrainingSessions
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.Status == SessionStatus.Scheduled);
            if (s == null || s.StartUtc <= DateTime.UtcNow) return NotFound();

            var strategy = _db.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                    var bookedNow = await _db.Bookings
                        .Where(b => b.SessionId == sessionId && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Pending))
                        .CountAsync();

                    if (bookedNow >= s.Capacity)
                    {
                        await tx.RollbackAsync();
                        throw new InvalidOperationException("This session just sold out.");
                    }

                    var dupNow = await _db.Bookings.AnyAsync(b => b.SessionId == sessionId
                                                                  && b.UserId == user.Id
                                                                  && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Pending));
                    if (dupNow)
                    {
                        await tx.RollbackAsync();
                        throw new InvalidOperationException("You already have a booking for this session.");
                    }

                    var booking = new Booking
                    {
                        SessionId = sessionId,
                        UserId = user.Id,
                        Status = BookingStatus.Booked,
                        CreatedUtc = DateTime.UtcNow
                    };
                    _db.Bookings.Add(booking);
                    await _db.SaveChangesAsync();

                    var payment = new Payment
                    {
                        BookingId = booking.Id,
                        Amount = s.Price,
                        Method = PaymentMethod.Cash,
                        Status = PaymentStatus.Pending,
                        Reference = $"CASH-{booking.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    };
                    _db.Payments.Add(payment);
                    await _db.SaveChangesAsync();

                    await tx.CommitAsync();
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            TempData["Msg"] = "Cash booking confirmed. Please pay at the session.";
            return RedirectToAction(nameof(Mine));
        }

        [HttpPost("payfast/start")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFastStart(int sessionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var s = await _db.TrainingSessions
                .Include(x => x.TrainingProgram)
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.Status == SessionStatus.Scheduled);
            if (s == null || s.StartUtc <= DateTime.UtcNow) return NotFound();

            Booking booking;
            string? reference = null; // <-- initialize

            try
            {
                var strategy = _db.Database.CreateExecutionStrategy();
                booking = await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                    var bookedNow = await _db.Bookings
                        .Where(b => b.SessionId == sessionId && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Pending))
                        .CountAsync();
                    if (bookedNow >= s.Capacity)
                    {
                        await tx.RollbackAsync();
                        throw new InvalidOperationException("This session just sold out.");
                    }

                    var dupNow = await _db.Bookings.AnyAsync(b => b.SessionId == sessionId
                                                                  && b.UserId == user.Id
                                                                  && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Pending));
                    if (dupNow)
                    {
                        await tx.RollbackAsync();
                        throw new InvalidOperationException("You already have a booking (or pending payment) for this session.");
                    }

                    var newBooking = new Booking
                    {
                        SessionId = sessionId,
                        UserId = user.Id,
                        Status = BookingStatus.Pending,
                        CreatedUtc = DateTime.UtcNow
                    };
                    _db.Bookings.Add(newBooking);
                    await _db.SaveChangesAsync();

                    // create once; reuse everywhere
                    reference = $"PF-{newBooking.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                    var payment = new Payment
                    {
                        BookingId = newBooking.Id,
                        Amount = s.Price,
                        Currency = "ZAR",
                        Method = PaymentMethod.PayFast,
                        Status = PaymentStatus.Pending,
                        Reference = reference
                    };
                    _db.Payments.Add(payment);
                    await _db.SaveChangesAsync();

                    await tx.CommitAsync();
                    return newBooking;
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            if (string.IsNullOrEmpty(reference))
                return BadRequest("Could not create a payment reference.");

            var returnUrl = Url.Action("PayFastReturn", "CustomerBookings", null, Request.Scheme);
            var cancelUrl = Url.Action("PayFastCancel", "CustomerBookings", null, Request.Scheme);
            var notifyUrl = Url.Action("PayFastNotify", "CustomerBookings", null, Request.Scheme);

            var itemName = $"{s.Title} ({s.TrainingProgram?.Name ?? "Session"})";
            var (actionUrl, fields) = _payfast.BuildOnceOffForm(
                reference: reference,                 // <-- same reference
                itemName: itemName,
                amount: s.Price,
                buyerEmail: user.Email ?? "",
                returnUrl: returnUrl,
                cancelUrl: cancelUrl,
                notifyUrl: notifyUrl
            );

            return View("PayFastRedirect", new PayFastRedirectVM { ActionUrl = actionUrl, Fields = fields });
        }

        [AllowAnonymous]
        [HttpGet("payfast/return")]
        public IActionResult PayFastReturn() => View("PayFastReturn");

        [AllowAnonymous]
        [HttpGet("payfast/cancel")]
        public IActionResult PayFastCancel()
        {
            TempData["Msg"] = "Payment cancelled.";
            return RedirectToAction("Index", "Schedule");
        }

        [AllowAnonymous]
        [HttpPost("payfast/notify")]
        public async Task<IActionResult> PayFastNotify()
        {
            var form = Request.HasFormContentType ? Request.Form : null;
            if (form == null) return Ok();

            var mPaymentId = form["m_payment_id"].ToString();
            var pfPaymentId = form["pf_payment_id"].ToString();
            var paymentStatus = form["payment_status"].ToString();

            if (string.IsNullOrWhiteSpace(mPaymentId)) return Ok();

            var payment = await _db.Payments
                .Include(p => p.Booking).ThenInclude(b => b.Session)
                .FirstOrDefaultAsync(p => p.Reference == mPaymentId);

            if (payment == null) return Ok();

            payment.ProviderReference = pfPaymentId;
            payment.RawPayload = string.Join("&", form.Select(kv => $"{kv.Key}={kv.Value}"));
            payment.UpdatedUtc = DateTime.UtcNow;

            var verified = await _itn.VerifyAsync(form, payment);
            if (!verified)
            {
                payment.Status = PaymentStatus.Failed;
                await _db.SaveChangesAsync();
                return Ok();
            }

            if (string.Equals(paymentStatus, "COMPLETE", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = PaymentStatus.Paid;
                payment.PaidUtc = DateTime.UtcNow;

                if (payment.Booking.Status == BookingStatus.Pending || payment.Booking.Status == BookingStatus.Cancelled)
                    payment.Booking.Status = BookingStatus.Booked;
            }
            else if (string.Equals(paymentStatus, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = PaymentStatus.Failed;
                if (payment.Booking.Status == BookingStatus.Pending)
                    payment.Booking.Status = BookingStatus.Cancelled;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _db.Bookings.AsNoTracking()
                .Include(b => b.Session).ThenInclude(s => s.TrainingProgram)
                .Include(b => b.Payments)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedUtc)
                .ToListAsync();

            return View(items);
        }

        [HttpPost("cancel/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var b = await _db.Bookings
                .Include(x => x.Session)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (b == null) return NotFound();
            if (b.Status != BookingStatus.Booked && b.Status != BookingStatus.Pending) return BadRequest();

            if (b.Session!.StartUtc <= DateTime.UtcNow.AddHours(24))
                return BadRequest("Too late to cancel online.");

            b.Status = BookingStatus.Cancelled;
            b.CancelledUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Booking canceled.";
            return RedirectToAction(nameof(Mine));
        }
    }
}