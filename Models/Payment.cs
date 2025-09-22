using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PassItOnAcademy.Models
{
    // Remove these enums if they already exist in your project
    public enum PaymentMethod { Cash = 0, PayFast = 1 }
    public enum PaymentStatus { Pending = 0, Paid = 1, Failed = 2, Cancelled = 3 }

    // Helpful indexes for common lookups
    [Index(nameof(BookingId))]
    [Index(nameof(Status))]
    [Index(nameof(Reference))]
    public class Payment
    {
        public int Id { get; set; }

        // ---- Relations ----
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; } = default!;

        // ---- Money ----
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "ZAR";

        // ---- Method & Status ----
        public PaymentMethod Method { get; set; } = PaymentMethod.PayFast;

        public PaymentStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                if (_status == PaymentStatus.Paid && PaidUtc is null)
                    PaidUtc = DateTime.UtcNow;
            }
        }
        private PaymentStatus _status = PaymentStatus.Pending;

        // ---- References used by controller/ITN ----
        // m_payment_id (your merchant reference)
        [MaxLength(64)]
        public string? Reference { get; set; }

        // pf_payment_id (gateway reference)
        [MaxLength(128)]
        public string? ProviderReference { get; set; }

        // Optional legacy aliases (keep if you already had them; else delete)
        [MaxLength(64)]
        public string? MerchantReference { get; set; }   // legacy alias of Reference

        [MaxLength(128)]
        public string? GatewayPaymentId { get; set; }    // legacy alias of ProviderReference

        // Signature verification result from ITN (if you implement it)
        public bool SignatureVerified { get; set; }

        // ---- Timestamps ----
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? PaidUtc { get; set; }      // set when Status -> Paid
        public DateTime? UpdatedUtc { get; set; }   // set on ITN/update

        // ---- Auditing / diagnostics ----
        [MaxLength(4000)]
        public string? RawPayload { get; set; }

        // ---- Admin notes ----
        [MaxLength(240)]
        public string? AdminNote { get; set; }

        // ---- Convenience (not mapped) ----
        [NotMapped]
        public bool IsPaid => Status == PaymentStatus.Paid;

        [NotMapped]
        public string? DisplayReference =>
            !string.IsNullOrWhiteSpace(Reference) ? Reference :
            !string.IsNullOrWhiteSpace(MerchantReference) ? MerchantReference :
            ProviderReference ?? GatewayPaymentId;
    }
}
