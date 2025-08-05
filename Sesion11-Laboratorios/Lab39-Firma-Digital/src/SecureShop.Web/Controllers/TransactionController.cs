using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureShop.Security;
using System.Security.Claims;

namespace SecureShop.Web.Controllers;

[Authorize]
public class TransactionController : Controller
{
    private readonly IDigitalSignatureService _signatureService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(
        IDigitalSignatureService signatureService,
        ILogger<TransactionController> logger)
    {
        _signatureService = signatureService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult CreatePurchase()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchase(PurchaseTransactionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var currentUserId = User.FindFirst("oid")?.Value ?? 
                               User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? 
                           User.FindFirst("preferred_username")?.Value ?? "unknown@example.com";

            // Crear transacción de compra
            var transaction = new PurchaseTransaction
            {
                CustomerId = currentUserId,
                CustomerEmail = userEmail,
                Items = model.Items.Select(item => new TransactionItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.Quantity * item.UnitPrice
                }).ToList(),
                PaymentMethod = model.PaymentMethod,
                ShippingAddress = model.ShippingAddress,
                StoreId = User.FindFirst("store_id")?.Value ?? "store-001"
            };

            // Calcular totales
            transaction.SubTotal = transaction.Items.Sum(i => i.TotalPrice);
            transaction.TaxAmount = transaction.SubTotal * 0.08m; // 8% tax
            transaction.TotalAmount = transaction.SubTotal + transaction.TaxAmount;

            // Firmar la transacción digitalmente
            var signedDocument = await _signatureService.SignDocumentAsync(transaction, "PurchaseTransaction");

            _logger.LogInformation("Transacción {TransactionId} firmada exitosamente por usuario {UserId}", 
                transaction.TransactionId, currentUserId);

            ViewBag.SignedDocument = signedDocument;
            ViewBag.Transaction = transaction;
            return View("TransactionSigned");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando y firmando transacción");
            ModelState.AddModelError("", "Error procesando la transacción: " + ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult VerifySignature()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifySignature(string documentId, string signature, string documentContent)
    {
        if (string.IsNullOrEmpty(documentId) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(documentContent))
        {
            ModelState.AddModelError("", "Todos los campos son requeridos");
            return View();
        }

        try
        {
            var signedDocument = new SignedDocument
            {
                DocumentId = documentId,
                Signature = signature,
                DocumentContent = documentContent,
                SigningCertificateThumbprint = "" // En un escenario real, esto se obtendría del documento
            };

            var isValid = await _signatureService.VerifySignatureAsync(signedDocument);
            var signatureInfo = await _signatureService.GetSignatureInfoAsync(signedDocument);

            ViewBag.IsValid = isValid;
            ViewBag.SignatureInfo = signatureInfo;
            ViewBag.SignedDocument = signedDocument;

            return View("VerificationResult");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando firma digital");
            ModelState.AddModelError("", "Error verificando la firma: " + ex.Message);
            return View();
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SignAdminAction(AdminActionViewModel model)
    {
        try
        {
            var currentUserId = User.FindFirst("oid")?.Value ?? 
                               User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? 
                           User.FindFirst("preferred_username")?.Value ?? "admin@example.com";

            var adminAction = new AdminAction
            {
                AdminUserId = currentUserId,
                AdminEmail = userEmail,
                ActionType = model.ActionType,
                EntityType = model.EntityType,
                EntityId = model.EntityId,
                Changes = model.Changes ?? new Dictionary<string, object>(),
                Reason = model.Reason,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };

            var signedDocument = await _signatureService.SignDocumentAsync(adminAction, "AdminAction");

            _logger.LogInformation("Acción administrativa {ActionId} firmada por {UserId}", 
                adminAction.ActionId, currentUserId);

            return Json(new { success = true, documentId = signedDocument.DocumentId, signature = signedDocument.Signature });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firmando acción administrativa");
            return Json(new { success = false, error = ex.Message });
        }
    }
}

// ViewModels para el controlador
public class PurchaseTransactionViewModel
{
    public List<TransactionItemViewModel> Items { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
}

public class TransactionItemViewModel
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class AdminActionViewModel
{
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Dictionary<string, object>? Changes { get; set; }
    public string Reason { get; set; } = string.Empty;
}