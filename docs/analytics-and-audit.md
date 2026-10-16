# HomeSphere — Analítica Comercial, KPIs y Registro de Auditoría (Semana 7)

## 1. Visión General
La Semana 7 incorpora visibilidad operativa ejecutiva y endurecimiento de seguridad mediante:
1. **Dashboard Comercial con KPIs Reales**: Fórmulas agregadas basadas estrictamente en transacciones confirmadas, excluyendo pedidos no pagados o cancelados.
2. **Sistema de Auditoría Operativa (`AuditLog`)**: Registro inmutable de eventos críticos administrativos con sanitización automática de secretos y datos confidenciales.

---

## 2. Fórmulas de Negocio y Reglas de Reconocimiento

### 2.1 Timezone y Periodo
- Todas las marcas de tiempo se almacenan y procesan en **UTC (`DateTime.UtcNow` / `DateTimeOffset.UtcNow`)**.
- Los filtros temporales aceptan `fromUtc` y `toUtc`. El valor por defecto computa una ventana móvil de los **últimos 30 días**.

### 2.2 Ventas Reconocidas (`TotalRevenue`)
Para evitar métricas infladas por pedidos abandonados o carritos no completados:
$$\text{Revenue} = \sum_{o \in \text{Orders}} o.\text{Total} \quad \text{donde } o.\text{Status} \in \{\text{Paid}, \text{Processing}, \text{Shipped}, \text{Delivered}\}$$

> [!NOTE]
> Las órdenes en estado `PendingPayment` y `Cancelled` **NO** forman parte del ingreso reconocido.

### 2.3 Ticket Promedio (AOV - Average Order Value)
$$\text{AverageTicket} = \frac{\text{TotalRevenue}}{\text{PaidOrdersCount}}$$
Si $\text{PaidOrdersCount} = 0$, $\text{AverageTicket} = \$0.00$.

### 2.4 Alertas de Inventario Crítico
- Variantes con $\text{StockQuantity} \le 5$ y cuyo producto padre esté en estado `Active`.
- Permite a los gestores de compras reaccionar proactivamente antes de que se produzca una rotura de stock.

---

## 3. Endurecimiento de Seguridad y Auditoría (`AuditLog`)

### 3.1 Sanitización de Secretos
Antes de persistir cualquier payload JSON en `audit_logs.details_json`, el servicio `AuditService` inspecciona recursivamente las claves y reemplaza valores sensibles con `"[REDACTED]"`.

Campos redactados automáticamente:
- `password`, `passwordhash`
- `token`, `refreshtoken`
- `secret`, `apikey`
- `cardnumber`, `cvv`
- `authorization`

### 3.2 Trazabilidad de Actor y Correlación
Cada entrada registra:
- `Action`: Código unívoco del evento (`ORDER_STATUS_CHANGED`, `SCENE_CREATED`, `SCENE_UPDATED`, `SCENE_DELETED`, `STOCK_ADJUSTMENT`, `SYSTEM_INITIALIZED`).
- `EntityName` y `EntityId`: Recurso afectado.
- `ActorId`, `ActorEmail` y `ActorIpAddress`.
- `CorrelationId`: Identificador único de solicitud para correlación cruzada de logs.

---

## 4. Endpoints de la API

| Método | Endpoint | Roles | Descripción |
|---|---|---|---|
| `GET` | `/api/admin/analytics/dashboard` | Admin, Manager | Resumen de KPIs, evolución diaria de ventas, pedidos por estado, top productos y alertas de bajo stock |
| `GET` | `/api/admin/audit-logs` | Admin | Consulta de eventos de auditoría con filtrado por entidad y límite |
