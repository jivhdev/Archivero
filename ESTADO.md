# Estado del proyecto

> Se actualiza al final de cada sesión. Es lo tercero que hay que leer (después de AGENTS.md y SPEC.md) para saber dónde quedamos.

## Última sesión
- Fecha: (todavía no empezó el desarrollo)
- Qué se hizo: nada — este es el punto de partida, recién generado desde el Método JA el 2026-09-12.

## Siguiente paso
- Primera sesión: hacer el setup completo descrito en `LEEME.md` (git, GitHub, registrar en el vault), y después empezar por REQ-001 (Primera configuración) de `SPEC.md`, en formato de entrega acotada.

## Decisiones abiertas / dudas para el usuario
Ver la sección "Open issues" de `SPEC.md`:
- Nombres finales de interfaz pendientes (no bloquean el desarrollo).
- Elección final de la licencia de código, pendiente de qué librería .NET de PDFium se use.
- Alcance exacto de la "personalización visual liviana".

## Riesgos o cosas frágiles a tener en cuenta
- El reconocimiento automático de Emisor/Tipo depende de patrones de coordenadas marcados a mano — un cambio de diseño en un documento de un proveedor puede romper un patrón existente (ya contemplado en REQ-002/REQ-003, pero es el punto más frágil del sistema).
