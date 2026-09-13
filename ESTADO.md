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
- REQ-001: mergeado. ✅ Probado por Javier, funciona.
- REQ-003 (identificar documento nuevo) implementado en la rama `feature/req-003-identificar-documento-nuevo` — **PR pendiente de abrir/probar**. Se adelantó a REQ-002 porque REQ-002 (coincidencia automática) necesita que ya existan configuraciones con patrones, y esas solo se crean con el asistente de REQ-003.
- **Falta que Javier pruebe el asistente a mano** (es la pieza más grande y riesgosa hasta ahora — un visor de PDF con marcado a mano por arrastre — y no se pudo probar de forma interactiva desde la sesión de IA, solo se verificó que compila, que los tests automáticos pasan, y que el flujo no-interactivo — detectar un PDF nuevo y listarlo en pendientes — funciona de punta a punta).
- REQ-003 **no está completo todavía** (a propósito, ver detalle abajo): falta el escenario "vincular a configuración existente" y el escenario "posponer" está simplificado (no persiste todavía lo ya tipeado). Antes de dar el requerimiento por cerrado hay que volver a esto.
- Después: completar REQ-003, y recién ahí REQ-002.

### REQ-001 — qué se construyó
- Proyecto WPF (.NET 8) creado en `src/Archivero`, solución `Archivero.sln`.
- SQLite (`Microsoft.Data.Sqlite`) para la tabla `Configuracion` (clave/valor), guardada en `%LocalAppData%\Archivero\archivero.db`.
- `Servicios/CarpetaObservadaService.cs`: sugiere nombre/ubicación por defecto, valida colisión, crea la carpeta y la marca como observada.
- `Vistas/OnboardingWindow.xaml(.cs)`: asistente de primera configuración.
- Se instaló el SDK de .NET 8 en esta máquina (no estaba presente).

### REQ-003 — qué se construyó (escenario principal)
- **PDF embebido**: `Servicios/Pdf/LectorPdf.cs`, envoltorio de Docnet.Core (PDFium, MIT) — renderiza páginas y extrae texto dentro de un rectángulo (fracción 0-1 de la página, independiente de resolución).
- **Marcado sobre el PDF**: `Vistas/VisorPdfConMarcado` (UserControl reutilizable) — click-and-drag sobre la página para marcar un rectángulo; se reutiliza para Emisor, Tipo, Fecha y campo de nombre de archivo (la misma herramienta para cualquier dato, como pide SPEC.md).
- **Modelo de datos nuevo**: `EntidadesConocidas` (Emisor/Tipo con autocompletado), `Configuraciones`, `PatronesReconocimiento`, `Marcas`, `Pendientes` (ver `Datos/BaseDeDatos.cs`).
- **Detección de formato/patrón de carpeta**: `Servicios/FormatoCarpetaService.cs` — mira las subcarpetas ya existentes en la carpeta de destino elegida y detecta Directo / Por año / Por año y mes, y cómo está escrito cada componente (`yyyy` vs `yy`, `MM` vs `MMMM`).
- **Guardado seguro**: `Servicios/ClasificadorService.cs` — copia, verifica (hash SHA-256), y recién ahí borra el original.
- **Vigilancia de la carpeta**: `Servicios/VigilanciaCarpetaService.cs` — `FileSystemWatcher` en tiempo real + escaneo al arrancar. Por ahora, **todo PDF nuevo pasa a pendientes** (la coincidencia automática contra configuraciones existentes es REQ-002).
- **Asistente**: `Vistas/IdentificarDocumentoWindow` — 5 pasos (Emisor/Tipo → Carpeta → Formato → Nombre de archivo → Confirmar), con Posponer/Cancelar disponibles en cualquier paso.
- `MainWindow` ahora muestra la lista real de "Pendientes por reconocer"; doble clic abre el asistente.
- Tests automáticos nuevos en `src/Archivero.Tests` (19, todos verdes): extracción por coordenadas (con PDFs reales generados en el propio test), detección/derivación de formato de carpeta, coincidencia exacta Emisor+Tipo.

### REQ-003 — qué falta (no dar el requerimiento por cerrado)
- Escenario "vincular a configuración existente" (cuando un proveedor cambia el diseño del documento): no implementado.
- Escenario "posponer": simplificado — el documento no se pierde (sigue en pendientes), pero lo ya tipeado/marcado no se guarda todavía para retomarlo después.
- Colisión de nombre de archivo al clasificar el documento actual: por ahora solo muestra un error simple, no las 4 opciones completas de REQ-002 (Revisar/Reemplazar/Pendiente/Excepción) — es esperable, ese flujo es explícitamente de REQ-002.

### Nota sobre la sincronización automática del vault
Hay un proceso en background (mencionado en `AGENTS.md`) que hace commits automáticos ("Sync automatico: ...") a este repo con la identidad de Javier, incluso a mitad de una sesión de trabajo. No es un problema — solo hace que a veces aparezca un commit intermedio con código a medio terminar en el historial. Ya pasó una vez que agarró un archivo interno del harness (`.claude/scheduled_tasks.lock`), que se sacó y se agregó a `.gitignore`.

## Decisiones abiertas / dudas para el usuario
Ver la sección "Open issues" de `SPEC.md`:
- Nombres finales de interfaz pendientes (no bloquean el desarrollo).
- Elección final de la licencia de código, pendiente de qué librería .NET de PDFium se use.
- Alcance exacto de la "personalización visual liviana".

## Riesgos o cosas frágiles a tener en cuenta
- El reconocimiento automático de Emisor/Tipo depende de patrones de coordenadas marcados a mano — un cambio de diseño en un documento de un proveedor puede romper un patrón existente (ya contemplado en REQ-002/REQ-003, pero es el punto más frágil del sistema).
