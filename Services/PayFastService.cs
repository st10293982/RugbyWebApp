using Microsoft.Extensions.Options;

namespace PassItOnAcademy.Services
{
    public class PayFastOptions
    {
        public bool UseSandbox { get; set; }
        public string MerchantId { get; set; } = "";
        public string MerchantKey { get; set; } = "";
        public string? Passphrase { get; set; }
        public string ReturnUrl { get; set; } = "";
        public string CancelUrl { get; set; } = "";
        public string NotifyUrl { get; set; } = "";
    }

    public interface IPayFastService
    {
        (string ActionUrl, Dictionary<string, string> Fields) BuildOnceOffForm(
        string reference, string itemName, decimal amount, string buyerEmail,
        string? returnUrl = null, string? cancelUrl = null, string? notifyUrl = null);
    }

    public class PayFastService : IPayFastService
    {
        private readonly PayFastOptions _opt;
        public PayFastService(IOptions<PayFastOptions> opt) => _opt = opt.Value;

        public (string ActionUrl, Dictionary<string, string> Fields) BuildOnceOffForm(
            string reference, string itemName, decimal amount, string buyerEmail,
            string? returnUrl = null, string? cancelUrl = null, string? notifyUrl = null)
        {
            var url = _opt.UseSandbox
                ? "https://sandbox.payfast.co.za/eng/process"
                : "https://www.payfast.co.za/eng/process";

            var fields = new Dictionary<string, string>
            {
                ["merchant_id"] = _opt.MerchantId,
                ["merchant_key"] = _opt.MerchantKey,

                // prefer overrides from the request; fall back to appsettings
                ["return_url"] = returnUrl ?? _opt.ReturnUrl,
                ["cancel_url"] = cancelUrl ?? _opt.CancelUrl,
                ["notify_url"] = notifyUrl ?? _opt.NotifyUrl,

                ["m_payment_id"] = reference,
                ["amount"] = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                ["item_name"] = itemName,
                ["email_address"] = buyerEmail ?? ""
            };

            return (url, fields);
        }
    }
}
