namespace Fiap.Workshop.Domain.Errors;

public static class ServiceOrderErrors
{
    public const string InvalidStatusTransition = "Cannot change status from {0} to {1}.";
    public const string OrderClosed = "This service order is closed and can no longer be modified.";
    public const string InvalidDiscount = "Discount must be between zero and the subtotal.";
    public const string PartNotFound = "Part was not found in this service order.";
    public const string ServiceNotFound = "Service was not found in this service order.";
    public const string InvalidQuantity = "Quantity must be greater than zero.";
    public const string InvalidUnitPrice = "Unit price cannot be negative.";
    public const string InvalidEstimatedDuration = "Estimated duration cannot be negative.";
}
