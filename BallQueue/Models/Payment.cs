namespace BallQueue.Models;

/// <summary>
/// Represents a payment record for a player.
/// Tracks when, how much, and if a player has paid the required fee.
/// </summary>
public class Payment
{
    /// <summary>
    /// Unique identifier for this payment record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the player who made the payment.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Navigation property for the player.
    /// </summary>
    public virtual Player? Player { get; set; }

    /// <summary>
    /// Amount paid in this transaction (RD$).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date and time when the payment was recorded.
    /// </summary>
    public DateTime PaymentDateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Optional notes about the payment (e.g., payment method, reference number).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// ID of the session during which this payment was made.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property for the session.
    /// </summary>
    public virtual Session? Session { get; set; }

    /// <summary>
    /// Returns a summary of the payment.
    /// </summary>
    public override string ToString() =>
        $"Payment: RD${Amount} on {PaymentDateTime:g}";
}
