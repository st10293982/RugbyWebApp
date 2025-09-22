using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Data
{
    // Identity user is ApplicationUser, so make the DbContext generic
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // --- Tables ---
        public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
        public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<TrainingMaterial> TrainingMaterials => Set<TrainingMaterial>();
        public DbSet<AcademySetting> AcademySettings => Set<AcademySetting>();
        public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // -------- ApplicationUser relations --------
            // User (Customer) 1..* Bookings
            b.Entity<Booking>()
                .HasOne(x => x.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User (Coach) 1..* Sessions
            b.Entity<TrainingSession>()
                .HasOne(s => s.Coach)
                .WithMany(u => u.SessionsCoached)
                .HasForeignKey(s => s.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional: who cancelled a booking (no cascade)
            b.Entity<Booking>()
                .HasOne(x => x.CancelledByUser)
                .WithMany()
                .HasForeignKey(x => x.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------- TrainingProgram ↔ TrainingSession --------
            b.Entity<TrainingSession>()
                .HasOne(s => s.TrainingProgram)
                .WithMany(p => p.Sessions)
                .HasForeignKey(s => s.TrainingProgramId)
                .OnDelete(DeleteBehavior.SetNull);

            // -------- Booking ↔ Payment --------
            b.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(bk => bk.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------- Session ↔ Announcement/Material --------
            b.Entity<Announcement>()
                .HasOne(a => a.Session)
                .WithMany(s => s.Announcements)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.SetNull);

            b.Entity<TrainingMaterial>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Materials)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Created-by user (no cascade)
            b.Entity<Announcement>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<TrainingMaterial>()
                .HasOne(m => m.CreatedByUser)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Personal assignment (optional, no cascade)
            b.Entity<TrainingMaterial>()
                .HasOne(m => m.AssignedToUser)
                .WithMany()
                .HasForeignKey(m => m.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------- Precision --------
            b.Entity<TrainingProgram>().Property(p => p.Price).HasColumnType("decimal(10,2)");
            b.Entity<TrainingSession>().Property(s => s.Price).HasColumnType("decimal(10,2)");
            b.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(10,2)");

            // -------- Defaults --------
            b.Entity<TrainingSession>().Property(s => s.Status)
                .HasDefaultValue(SessionStatus.Scheduled);

            b.Entity<Booking>().Property(x => x.Status)
                .HasDefaultValue(BookingStatus.Booked);

            b.Entity<Payment>().Property(x => x.Status)
                .HasDefaultValue(PaymentStatus.Pending);

            b.Entity<Payment>().Property(p => p.Method)
                .HasDefaultValue(PaymentMethod.PayFast);

            // DB-side UTC timestamps (note: if your entities also set CLR defaults,
            // EF will send those values and the DB defaults won't fire)
            b.Entity<Booking>().Property(x => x.CreatedUtc)
                .HasDefaultValueSql("GETUTCDATE()");
            b.Entity<Payment>().Property(x => x.CreatedUtc)
                .HasDefaultValueSql("GETUTCDATE()");
            b.Entity<Announcement>().Property(x => x.CreatedUtc)
                .HasDefaultValueSql("GETUTCDATE()");
            b.Entity<TrainingMaterial>().Property(x => x.CreatedUtc)
                .HasDefaultValueSql("GETUTCDATE()");

            // -------- Indexes --------
            b.Entity<TrainingSession>().HasIndex(s => s.StartUtc);
            b.Entity<Booking>().HasIndex(x => new { x.UserId, x.CreatedUtc });
            b.Entity<Payment>().HasIndex(x => new { x.BookingId, x.Status });

            // Extra helpful lookups for PayFast/ITN and UI
            b.Entity<Payment>().HasIndex(p => p.Reference);
            b.Entity<Payment>().HasIndex(p => p.ProviderReference);
        }
    }
}
