using System.ComponentModel.DataAnnotations;

namespace SecureShop.Security;

// Modelo para transacciones de compra
public class PurchaseTransaction
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<TransactionItem> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
}

public class TransactionItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

// Modelo para cambios administrativos
public class AdminAction
{
    public string ActionId { get; set; } = Guid.NewGuid().ToString();
    public string AdminUserId { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, APPROVE
    public string EntityType { get; set; } = string.Empty; // PRODUCT, USER, ORDER, DISCOUNT
    public string EntityId { get; set; } = string.Empty;
    public Dictionary<string, object> Changes { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

// Modelo para aprobación de descuentos
public class DiscountApproval
{
    public string ApprovalId { get; set; } = Guid.NewGuid().ToString();
    public string DiscountCode { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public decimal MaxDiscountAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public string ApproverEmail { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    public string BusinessJustification { get; set; } = string.Empty;
    public string[] ApplicableProducts { get; set; } = Array.Empty<string>();
    public int MaxUsageCount { get; set; }
}