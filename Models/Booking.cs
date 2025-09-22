namespace PassItOnAcademy.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int SessionId { get; set; }
        public TrainingSession Session { get; set; } = default!;

        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public BookingStatus Status { get; set; } = BookingStatus.Booked;
        public DateTime? CancelledUtc { get; set; }

        public string? CancelledByUserId { get; set; }
        public ApplicationUser? CancelledByUser { get; set; }

        public int Quantity { get; set; } = 1; // seats; keep 1 for 1-on-1 now
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
