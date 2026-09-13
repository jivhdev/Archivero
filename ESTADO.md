# Estado del proyecto

> Se actualiza al final de cada sesión. Es lo tercero que hay que leer (después de AGENTS.md y SPEC.md) para saber dónde quedamos.

## Última sesión
- Fecha: 2026-09-12
- Qué se hizo: setup de primera sesión completo según `LEEME.md`:
  - `git init` + primer commit de la semilla.
  - Repo nuevo en GitHub: https://github.com/jivhdev/Archivero (privado).
  - Nota: ya existía un repo `jivhdev/archivero` distinto (versión en Python, con función de copiar datos a un ERP externo — fuera del `SPEC.md` actual). Se renombró y archivó (solo lectura) como `jivhdev/archivero-viejo-python` para no perder ese trabajo previo. Decisión tomada por Javier en el chat, no es un Caso-N.
  - Registrado en `C:\MQD\Scripts\otros-repos.txt` y actualizada la fila de Archivero en `C:\MQD\01-Proyectos\Registro-de-Proyectos.md`.
  - Creada la carpeta `preguntas/` vacía.

## Siguiente paso
- Empezar por REQ-001 (Primera configuración: elegir/crear la carpeta observada) de `SPEC.md`, en formato de entrega acotada.

## Decisiones abiertas / dudas para el usuario
Ver la sección "Open issues" de `SPEC.md`:
- Nombres finales de interfaz pendientes (no bloquean el desarrollo).
- Elección final de la licencia de código, pendiente de qué librería .NET de PDFium se use.
- Alcance exacto de la "personalización visual liviana".

## Riesgos o cosas frágiles a tener en cuenta
- El reconocimiento automático de Emisor/Tipo depende de patrones de coordenadas marcados a mano — un cambio de diseño en un documento de un proveedor puede romper un patrón existente (ya contemplado en REQ-002/REQ-003, pero es el punto más frágil del sistema).
