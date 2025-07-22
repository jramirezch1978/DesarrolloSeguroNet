using MediatR;
using SecureBank.Shared.DTOs;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Application.Features.Transactions.Queries.GetPaymentHistory;

/// <summary>
/// Query para obtener el historial de pagos
/// </summary>
public class GetPaymentHistoryQuery : IRequest<BaseResponse>
{
    /// <summary>
    /// ID del usuario
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID de la cuenta (opcional)
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Fecha desde
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Fecha hasta
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Número de página
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Tamaño de página
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
} 