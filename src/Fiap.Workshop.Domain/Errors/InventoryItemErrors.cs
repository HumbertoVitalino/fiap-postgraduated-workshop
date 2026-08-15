namespace Fiap.Workshop.Domain.Errors;

public static class InventoryItemErrors
{
    public const string InvalidQuantityOnHand = "Quantity on hand cannot be negative.";
    public const string InvalidReservedQuantity = "Reserved quantity cannot be negative.";
    public const string ReservedQuantityExceedsQuantityOnHand = "Reserved quantity cannot exceed quantity on hand.";
    public const string InvalidMinimumStock = "Minimum stock cannot be negative.";
    public const string InvalidUnitPrice = "Unit price cannot be negative.";
}
