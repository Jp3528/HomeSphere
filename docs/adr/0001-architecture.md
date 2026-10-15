# ADR-0001: Arquitectura Base, Observabilidad y Manejo de Errores de HomeSphere

## Estado
Aceptado

## Contexto
HomeSphere es un e-commerce avanzado para el sector hogar y decoración con una funcionalidad diferencial: ambientes interactivos con hotspots vinculados al catálogo real. El objetivo es construir una solución robusta, escalable y mantenible para prácticas profesionales de Ingeniería de Sistemas.

## Decisión
Se decide implementar una arquitectura de **Monolito Modular / Clean Architecture Pragmática** con estándares estrictos de observabilidad y contratos API:

1. **Frontend**: Next.js 16 (TypeScript, App Router, React Server Components para catálogo/SEO y Client Components para ambientes y checkout).
   - Cliente HTTP tipado (`apiClient`) centralizado en `src/lib/api-client.ts`.
   - Manejo de excepciones tipado (`ApiClientError`) deserializando respuestas RFC 7807 (`ProblemDetails`).

2. **Backend**: ASP.NET Core 8 Web API estructurado en 4 capas:
   - `HomeSphere.Domain`: Reglas de negocio puras, entidades de dominio, excepciones de dominio (`DomainException`, `NotFoundException`, `ConflictException`, `BusinessRuleValidationException`). Sin dependencias externas.
   - `HomeSphere.Application`: Casos de uso (comandos/consultas), DTOs, validaciones, interfaces de abstracción y modelo estándar de paginación (`PagedList<T>`).
   - `HomeSphere.Infrastructure`: Implementaciones técnicas, Entity Framework Core 8, persistencia PostgreSQL 17 (`HomeSphereDbContext`), adaptadores de almacenamiento y pasarelas mock.
   - `HomeSphere.WebApi`: Controladores REST, middleware de trazabilidad (`CorrelationIdMiddleware`), manejador global de excepciones (`GlobalExceptionHandler`), autenticación JWT, configuración de OpenAPI y health checks (`/health`, `/health/status`).

3. **Trazabilidad y Observabilidad**:
   - Todo request HTTP recibe o genera un encabezado `X-Correlation-Id`.
   - El correlation ID se propaga en los logs estructurados y en las extensiones del cuerpo de error de `ProblemDetails`.
   - Ningún log puede contener contraseñas, tokens JWT ni números de tarjeta de crédito.

4. **Persistencia**: PostgreSQL con migraciones versionadas de EF Core y tablas en convención `snake_case`.

5. **Contratos REST y Paginación**:
   - Paginación server-side mediante `PagedList<T>` (PageNumber, PageSize, TotalCount, TotalPages).
   - Respuestas de error estandarizadas bajo el estándar RFC 7807 (`ProblemDetails`).

## Consecuencias
- **Positivas**: Evita la complejidad operacional y de red de microservicios prematuros, facilita transacciones ACID en pedidos e inventario, provee trazabilidad completa de errores extremo a extremo entre cliente y servidor.
- **Negativas / Desafíos**: Requiere disciplina estricta de inversión de dependencias para no acoplar la capa de dominio a detalles de infraestructura.
