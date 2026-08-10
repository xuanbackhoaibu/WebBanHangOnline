using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHangOnline.Models;

public class PaymentTransaction
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [MaxLength(32)]
    public string Provider { get; set; } = string.Empty;

    [MaxLength(128)]
    public string TransactionCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = PaymentStatuses.Unpaid;

    public bool IsSignatureValid { get; set; }

    public string RawPayload { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
