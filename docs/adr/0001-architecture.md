# ADR-0001: Arquitectura Base de HomeSphere

## Estado
Aceptado

## Contexto
HomeSphere es un e-commerce avanzado para el sector hogar y decoración con una funcionalidad diferencial: ambientes interactivos con hotspots vinculados al catálogo real. El objetivo es construir una solución robusta, escalable y mantenible para prácticas profesionales de Ingeniería de Sistemas.

## Decisión
Se decide implementar una arquitectura de **Monolito Modular / Clean Architecture Pragmática**:
1. **Frontend**: Next.js (TypeScript, App Router, React Server Components para catálogo/SEO y Client Components para ambientes y checkout).
2. **Backend**: ASP.NET Core 8 Web API estructurado en 4 capas:
   - `HomeSphere.Domain`: Reglas de negocio puras, entidades de dominio, excepciones de dominio. Sin dependencias externas.
   - `HomeSphere.Application`: Casos de uso (comandos/consultas), DTOs, validaciones, interfaces de abstracción.
   - `HomeSphere.Infrastructure`: Implementaciones técnicas, Entity Framework Core 8, persistencia PostgreSQL, adaptadores de almacenamiento y pasarelas mock.
   - `HomeSphere.WebApi`: Controladores REST, filtros de idempotencia, autenticación JWT, configuración de OpenAPI y middleware global de excepciones con `ProblemDetails`.
3. **Persistencia**: PostgreSQL 16 con migraciones versionadas de EF Core.
4. **Desacoplamiento**: El frontend y backend se comunican exclusivamente a través de contratos REST tipados con DTOs (nunca exponiendo entidades de dominio directamente).

## Consecuencias
- **Positivas**: Evita la complejidad operacional y de red de microservicios prematuros, facilita transacciones ACID en pedidos e inventario, y mantiene límites limpios entre responsabilidades.
- **Negativas / Desafíos**: Requiere disciplina estricta de inversión de dependencias para no acoplar la capa de dominio a detalles de infraestructura.
