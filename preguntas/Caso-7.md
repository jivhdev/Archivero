# Caso-7 — Imprimir después de archivar + ajustes de ventana (2026-09-17)

*Resuelto en el vault del Método JA. Los dos puntos son independientes entre sí.*

## 1. "Imprimir después de archivar" — nueva opción configurable por tipo de documento

Igual mecanismo que "abrir después de guardar" (Caso-1, punto 5), pero para imprimir. Por tipo de documento configurado, el usuario puede activar que Archivero lo mande a imprimir apenas lo archiva.

**Reglas:**
- Arranca siempre **desactivada** por defecto — igual que "abrir después de guardar". Se activa con un click, se puede desactivar en cualquier momento (vía "editar configuración", como cualquier otro ajuste de esa configuración).
- Al activarla, el usuario elige uno de tres modos de impresión:
  1. **Siempre solo la primera página.**
  2. **Preguntar cada vez** (al momento de imprimir) si imprimir solo la primera página o más.
  3. **Siempre todas las páginas.**

**Selección de impresora — configuración aparte y global (no por tipo de documento):**
Agregar un lugar de configuración general (ej. dentro de "Administrar clasificaciones", o una pantalla de ajustes si conviene crear una) donde el usuario elige qué impresora usar para todo lo que Archivero mande a imprimir con esta función. Si el usuario nunca la toca, usar la impresora predeterminada de Windows. Es una sola elección de impresora para toda la app, no una por cada tipo de documento.

## 2. Ventanas siempre maximizadas + verificar que la ubicación se vea completa

- Todas las ventanas de Archivero deben abrir **maximizadas** por defecto siempre — incluida la ventana principal (`MainWindow`), no solo las del flujo de identificación de documentos sin texto.
- Con esto, la ruta de ubicación que hoy se ve cortada en el flujo de sin-texto debería tener espacio suficiente para mostrarse completa — verificarlo a mano después del cambio y confirmar en `ESTADO.md`; si todavía queda cortada en algún lugar puntual, corregir ese control específico (ej. permitir que el texto haga wrap, o ensanchar el control).

## Cierre del caso

Una vez implementados y verificados a mano los 2 puntos, regenerar el `.exe` con `dotnet publish` (self-contained, ver comando en `GIT.md`) para dejar la distribución actualizada.
