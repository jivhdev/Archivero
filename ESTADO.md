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
- REQ-003 (identificar documento nuevo) en la rama `feature/req-003-identificar-documento-nuevo`, PR abierto: https://github.com/jivhdev/Archivero/pull/2. Se adelantó a REQ-002 porque REQ-002 (coincidencia automática) necesita que ya existan configuraciones con patrones, y esas solo se crean con el asistente de REQ-003.
- Los 3 escenarios de SPEC.md ya están implementados: crear configuración nueva, vincular a una existente, y posponer con progreso guardado.
  - **Probados por Javier y funcionando**: crear configuración nueva, vincular a una existente (con varios bugs reales encontrados y arreglados en el camino — ver commits de la rama: coordenadas corridas, sin preview del texto extraído, bug de guardado duplicado por UNIQUE constraint, texto de la UI pasado a español neutro).
  - **Todavía sin probar a mano**: "posponer con progreso guardado" (implementado en el último commit de la rama, compila y pasa los tests, pero falta que Javier lo pruebe de verdad antes de mergear — abrir un documento, marcar algo, cerrar sin terminar, y volver a abrirlo para confirmar que se restaura).
- Después de que Javier confirme "posponer", mergear el PR y arrancar REQ-002.

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

### REQ-003 — escenario "vincular a configuración existente"
Implementado: si el Emisor+Tipo que se está identificando ya tiene una configuración guardada, el asistente ofrece vincular el documento actual a ella como patrón de reconocimiento adicional (en vez de intentar crear una segunda configuración, que rompía con un error de SQLite). La carpeta/formato/nombre quedan fijados por la configuración existente; igual hay que marcar Fecha y/o el campo de nombre si esa configuración los usa, porque las coordenadas son específicas del diseño de cada documento.

### REQ-003 — escenario "posponer con progreso guardado"
Implementado: tabla `Borradores` (`Datos/BorradorRepository.cs`), clave = ruta del archivo pendiente, valor = JSON con lo tipeado/marcado hasta el momento. Se guarda al tocar "Posponer" o al cerrar la ventana sin terminar (la X, tratado igual que posponer per SPEC.md); se restaura automáticamente la próxima vez que se abre ese mismo documento, con un aviso. "Cancelar" (con confirmación) descarta el borrador; guardar con éxito también lo limpia. **Falta que Javier lo pruebe a mano.**

### REQ-003 — qué queda fuera a propósito (no es un defecto)
- Colisión de nombre de archivo al clasificar el documento actual: por ahora solo muestra un error simple, no las 4 opciones completas de REQ-002 (Revisar/Reemplazar/Pendiente/Excepción) — ese flujo es explícitamente de REQ-002.

### Nota sobre la sincronización automática del vault
Hay un proceso en background (mencionado en `AGENTS.md`) que hace commits automáticos ("Sync automatico: ...") a este repo con la identidad de Javier, incluso a mitad de una sesión de trabajo. No es un problema — solo hace que a veces aparezca un commit intermedio con código a medio terminar en el historial. Ya pasó una vez que agarró un archivo interno del harness (`.claude/scheduled_tasks.lock`), que se sacó y se agregó a `.gitignore`.

## Decisiones abiertas / dudas para el usuario
Ver la sección "Open issues" de `SPEC.md`:
- Nombres finales de interfaz pendientes (no bloquean el desarrollo).
- Elección final de la licencia de código, pendiente de qué librería .NET de PDFium se use.
- Alcance exacto de la "personalización visual liviana".

## Riesgos o cosas frágiles a tener en cuenta
- El reconocimiento automático de Emisor/Tipo depende de patrones de coordenadas marcados a mano — un cambio de diseño en un documento de un proveedor puede romper un patrón existente (ya contemplado en REQ-002/REQ-003, pero es el punto más frágil del sistema).
