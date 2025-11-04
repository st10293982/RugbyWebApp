// Services/PayFastItnVerifier.cs
using Microsoft.Extensions.Options;
using PassItOnAcademy.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

public interface IPayFastItnVerifier
{
    Task<bool> VerifyAsync(IFormCollection form, Payment payment);
}

public class PayFastItnVerifier : IPayFastItnVerifier
{
    private readonly HttpClient _http;
    private readonly PassItOnAcademy.Services.PayFastOptions _opt;

    public PayFastItnVerifier(IHttpClientFactory httpClientFactory, IOptions<PassItOnAcademy.Services.PayFastOptions> opt)
    {
        _http = httpClientFactory.CreateClient();
        _opt = opt.Value;
    }

    public async Task<bool> VerifyAsync(IFormCollection form, Payment payment)
    {
        // 1) Build sorted query (exclude 'signature')
        var keyValues = form
            .Where(kv => !string.Equals(kv.Key, "signature", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}");

        var query = string.Join("&", keyValues);

        // 2) Append passphrase if present
        var withPassphrase = string.IsNullOrWhiteSpace(_opt.Passphrase)
            ? query
            : $"{query}&passphrase={WebUtility.UrlEncode(_opt.Passphrase)}";

        // 3) Compute MD5 signature
        using var md5 = MD5.Create();
        var bytes = Encoding.ASCII.GetBytes(withPassphrase);
        var hash = md5.ComputeHash(bytes);
        var computedSignature = string.Concat(hash.Select(b => b.ToString("x2")));
        var sentSignature = form["signature"].ToString();
        if (!string.Equals(computedSignature, sentSignature, StringComparison.OrdinalIgnoreCase))
            return false;

        // 4) Server-to-server validation (derive from UseSandbox)
        var validateUrl = _opt.UseSandbox
            ? "https://sandbox.payfast.co.za/eng/query/validate"
            : "https://www.payfast.co.za/eng/query/validate";

        using var content = new StringContent(query, Encoding.ASCII, "application/x-www-form-urlencoded");
        var resp = await _http.PostAsync(validateUrl, content);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode || !body.Contains("VALID", StringComparison.OrdinalIgnoreCase))
            return false;

        // 5) Amount & currency checks
        var grossOk = decimal.TryParse(form["amount_gross"], NumberStyles.Any, CultureInfo.InvariantCulture, out var gross);
        var currency = form["currency"].ToString();
        if (!grossOk || gross != payment.Amount || !string.Equals(currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}