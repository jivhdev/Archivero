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
- REQ-001, REQ-003, REQ-002, REQ-004: **mergeados**. ✅ Todos probados por Javier y funcionando (varios bugs reales encontrados y arreglados en el camino — ver secciones de cada uno más abajo: coordenadas corridas, coincidencia contra el nombre en vez del texto marcado, parser de fechas que no soportaba varios formatos comunes).
- REQ-005 (carcasa — bandeja del sistema, minimizar en vez de cerrar, instancia única, ícono que avisa pendientes) en la rama `feature/req-005-carcasa-bandeja`, PR abierto: https://github.com/jivhdev/Archivero/pull/5. **Es el último requerimiento funcional de SPEC.md.**
  - Verificado desde la sesión de IA: instancia única (abrir el ejecutable dos veces solo deja una corriendo), build y arranque sin errores.
  - **Falta que Javier pruebe a mano** todo lo visual (parpadeo del ícono, minimizar con la X, volver desde la bandeja, "Salir") — no se puede hacer clic en una ventana real desde esta sesión. Pasos exactos en el PR.
- Cuando Javier confirme REQ-005, **los 5 requerimientos funcionales de SPEC.md quedan completos**. Después de eso: revisar juntos qué falta para un hito de empaquetado (ejecutable autocontenido) y las decisiones abiertas (licencia, nombres de interfaz) — ver sección de abajo.

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

### REQ-002 — qué se construyó
- **Coincidencia automática**: `Servicios/CoincidenciaAutomaticaService.cs` — cuando llega un PDF, prueba los patrones de reconocimiento de todas las configuraciones guardadas: extrae texto en la coordenada de Emisor y de Tipo de cada patrón y compara exacto (sin distinguir mayúsculas/espacios) contra el Emisor+Tipo de su propia configuración.
- **Guardado automático**: `Servicios/GuardadoAutomaticoService.cs` — si un patrón coincide, extrae Fecha y/o Nombre de archivo según corresponda, valida que tengan forma válida, y clasifica el archivo. Devuelve un resultado tipado (Guardado / ValorInvalido / Duplicado / CarpetaNoDisponible) para que quien lo llama decida qué hacer.
- **`VigilanciaCarpetaService` actualizado**: ahora intenta la coincidencia automática antes de mandar algo a pendientes; maneja explícitamente PDF sin texto extraíble (no se toca) y archivo corrupto/no legible (se ignora ese archivo puntual, sigue observando los demás — RNF-2).
- **Cambio de modelo**: `ConfiguracionDocumento.Marcas` (lista plana) pasó a ser `Patrones` (`List<PatronReconocimiento>`), porque la coincidencia automática necesita probar cada patrón (cada diseño de documento) por separado, no mezclar las marcas de todos.
- `MainWindow` ahora también muestra un historial "Guardados automáticamente".
- Tests nuevos: `CoincidenciaAutomaticaServiceTests` y `GuardadoAutomaticoServiceTests` (30 tests en total, todos verdes).

### REQ-002 — qué queda fuera a propósito (no es un defecto)
- Colisión de nombre de archivo: hoy solo evita sobreescribir (manda a pendientes). Faltan las 4 opciones completas de SPEC.md (Revisar lado a lado / Reemplazar / Dejar pendiente / Guardar como excepción) — es una pieza de UI aparte, no bloquea el resto de REQ-002.

### REQ-002 — bug real encontrado por Javier y arreglado: coincidencia contra el texto marcado, no contra el nombre
Caso real: el nombre del Emisor no siempre es texto extraíble (a veces es un logo/imagen). La única forma de identificar el documento con certeza es marcar otro campo confiable (ej. el RUT) mientras se sigue escribiendo el nombre real a mano para mostrar/organizar. La coincidencia automática original comparaba el texto extraído contra el NOMBRE tipeado, no contra lo que esa coordenada realmente contiene — nunca iba a reconocer nada si el campo marcado no era literalmente el nombre.

Fix: se agregó `Marca.TextoReferencia` (texto extraído en el momento de crear la marca); la coincidencia automática compara contra este valor, no contra el nombre de la entidad (con fallback al comportamiento viejo si una marca no tiene el dato, para no romper configuraciones creadas antes del fix — igual hay que re-vincular esas para que el fix las alcance). Incluye migración mínima de esquema (`ALTER TABLE ... ADD COLUMN` si falta, sin perder datos). Ver `Servicios/CoincidenciaAutomaticaService.cs` y memoria `feedback-coincidencia-contra-texto-marcado`.

### REQ-004 — qué se construyó
- **"Administrar clasificaciones"** (botón en `MainWindow`): lista todas las configuraciones (Emisor, Tipo, carpeta destino, disponibilidad), buscador con autocompletado (`Vistas/AdministrarClasificacionesWindow`).
- **"Revisar disponibilidad"**: chequea `Directory.Exists` de cada carpeta destino, solo al abrir o al tocar el botón (sin polling en background, como pide SPEC.md).
- **Editar (reutiliza el asistente paso a paso)**: feedback de Javier — prefiere revisar/corregir desde el mismo asistente de identificación, no un formulario aparte. "Editar" abre `IdentificarDocumentoWindow` en modo edición: si la configuración tiene más de un patrón (ej. guía vieja/nueva de un proveedor), primero se elige cuál con `Vistas/ElegirPatronWindow` (mostrando el texto marcado como Emisor de cada uno); como el documento original ya se movió, se pide un PDF de ejemplo del mismo diseño para revisar/corregir las marcas sobre él, sin tocarlo ni moverlo. Emisor/Tipo quedan de solo lectura. "Guardar cambios" usa `ActualizarPatron` + `ActualizarDestino` — nunca reclasifica archivos ya guardados.
- **Exportar/respaldar**: vuelca todas las configuraciones+patrones a un JSON, en un botón secundario de la ventana (no en la pantalla principal).
- Tests nuevos: `ActualizarDestino`, `ActualizarPatron`, `ObtenerTodas`.

### REQ-002/REQ-003 — bug real: parser de fechas y variante de carpeta año+yyyyMM
Javier encontró que `"30-ABR-2026"` (día-mes en letras abreviado-año) no lo reconocía el parseo de fecha por defecto. Se agregó `Servicios/FechaExtraidaService.cs`: prueba varios formatos numéricos explícitos, fechas con nombre de mes en español (tabla propia de meses/abreviaturas, no depende de la configuración regional del sistema) en dos formas ("30 de abril de 2026" y "30-ABR-2026"), y como último recurso el parseo general de .NET. Se usa tanto en el asistente como en el guardado automático.

También se agregó la variante de patrón de carpeta `yyyy\yyyyMM` (año en una subcarpeta, mes concatenado con el año dentro — ej. `2026\202601`), pedida por Javier por ser una convención muy usada; se detecta sola y aparece como opción al elegir "por año y mes". 54 tests en total.

### REQ-005 — qué se construyó
- **Cerrar no apaga**: la X de `MainWindow` la minimiza a la bandeja en vez de cerrar Archivero (`OnClosing` cancela y hace `Hide()`).
- **Ícono de bandeja** (`Servicios/TrayIconService.cs`, dibujado en código con `System.Drawing`, sin archivo `.ico`): parpadea azul↔naranja mientras haya algo en pendientes, sin popups ni sonidos; deja de parpadear cuando la lista queda vacía.
- **Instancia única**: `Mutex` con nombre fijo en `App.xaml.cs`; si ya hay una instancia corriendo, la nueva apertura no crea nada — usa `FindWindow`/`SetForegroundWindow` (Win32, vía P/Invoke) para enfocar la ventana existente (aunque esté oculta en la bandeja) y se cierra sola. Verificado desde la sesión: abrir el ejecutable dos veces deja una sola instancia corriendo.
- **Aviso si la carpeta observada desaparece**: se engancha al evento `Error` del `FileSystemWatcher` (se dispara si el directorio se borra/mueve) y avisa en vez de fallar en silencio.
- **Falta que Javier pruebe a mano** lo visual (parpadeo real, minimizar/restaurar, "Salir") — no se puede interactuar con una ventana real desde la sesión de IA.

### Nota sobre la sincronización automática del vault
Hay un proceso en background (mencionado en `AGENTS.md`) que hace commits automáticos ("Sync automatico: ...") a este repo con la identidad de Javier, incluso a mitad de una sesión de trabajo. No es un problema — solo hace que a veces aparezca un commit intermedio con código a medio terminar en el historial. Ya pasó una vez que agarró un archivo interno del harness (`.claude/scheduled_tasks.lock`), que se sacó y se agregó a `.gitignore`.

## Decisiones abiertas / dudas para el usuario
Ver la sección "Open issues" de `SPEC.md`:
- Nombres finales de interfaz pendientes (no bloquean el desarrollo).
- Elección final de la licencia de código, pendiente de qué librería .NET de PDFium se use.
- Alcance exacto de la "personalización visual liviana".

## Riesgos o cosas frágiles a tener en cuenta
- El reconocimiento automático de Emisor/Tipo depende de patrones de coordenadas marcados a mano — un cambio de diseño en un documento de un proveedor puede romper un patrón existente (ya contemplado en REQ-002/REQ-003, pero es el punto más frágil del sistema).
