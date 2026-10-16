namespace HomeSphere.Domain.Enums;

public enum InventoryMovementType
{
    Receipt = 1,              // Ingreso de mercadería / compra
    Adjustment = 2,           // Ajuste manual por conteo o merma
    ReservationRelease = 3,   // Liberación de stock por reserva expirada o cancelada
    Sale = 4,                 // Venta confirmada
    Return = 5                // Devolución de cliente
}
