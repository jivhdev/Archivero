# Caso-1 — Hallazgos del piloto real (2026-09-14)

*Resuelto en el vault del Método JA. Pegar este archivo en `preguntas/Caso-1.md` del repositorio de código.*

Javier usó Archivero en su flujo real de trabajo y encontró 5 cosas que hay que corregir/agregar antes de repartirlo a sus compañeros. Ninguna contradice `SPEC.md` — lo amplían o corrigen bugs reales. Un requerimiento a la vez, como siempre, y actualizá `ESTADO.md` al cerrar.

## 1. PDFs sin texto extraíble no pueden quedar invisibles (afecta REQ-002)

**Bug:** hoy, si llega un PDF sin texto extraíble, Archivero no lo procesa Y ADEMÁS no lo muestra en ningún lado — desaparece de la vista sin dejar rastro.

**Corrección:** debe aparecer como pendiente, igual que cualquier documento no reconocido. El usuario tiene que poder configurarle un destino: Emisor y Tipo tipeados a mano (no hay texto para marcar por coordenadas), carpeta y formato de guardado como cualquier configuración normal. Los siguientes documentos del mismo tipo se enrutan solos a partir de esa configuración — nunca va a haber auto-extracción de campos en ellos (no hay texto), pero sí ubicación automática.

## 2. Reconocimiento de formato de carpeta: por evidencia real, nunca por adivinanza (afecta REQ-003)

**Bug real y grave:** Javier eligió la carpeta EXACTA de destino para un documento, y Archivero de todos modos creó una subcarpeta de año/mes adentro, sin que nadie la pidiera.

**Rediseño:**
- Al elegir la carpeta de destino, Archivero mira las subcarpetas que **ya existen** ahí y busca el patrón real comparando varios ejemplos entre sí (qué parte del nombre cambia — la fecha — y qué parte es fija). Esto reemplaza cualquier detector limitado a un set fijo de opciones (directo/año/año-mes): tiene que poder reconocer cualquier convención real en uso (trimestre, semana, lo que sea), generalizando a partir de la evidencia, no de una lista que nosotros armamos de antemano.
- **Regla de seguridad, no negociable:** si no hay evidencia clara de un patrón (carpeta vacía, o nada con fecha reconocible adentro), Archivero **nunca inventa una subcarpeta por su cuenta**. El default es guardar directo en la carpeta exacta elegida. Crear una subdivisión nueva es siempre una decisión activa del usuario, jamás una suposición del programa. Esto es exactamente lo que falló.
- Una vez que un patrón queda **confirmado** (por evidencia real y aprobado en el preview del punto 3, o definido a mano si no había evidencia), Archivero sí puede ofrecer crear la carpeta del próximo período automáticamente cuando llegue el momento — reemplazando lo que Javier hace hoy a mano cada mes. Esto no es "inventar": es continuar un patrón ya confirmado por el usuario.
- Al aplicar un patrón ya confirmado a un documento nuevo, ofrecer las opciones paso a paso (nunca un formulario con varios campos juntos de una vez): guardar en el período actual (si esa carpeta ya existe), crear el período actual si falta (ofreciendo ahí mismo también crear por adelantado el período siguiente), o elegir la subcarpeta manualmente como alternativa. El nombre de cada período lo pone el usuario — "por año", "por mes" son ejemplos del entorno de Javier, no palabras fijas que el programa deba usar.
- Si no hay ningún patrón detectable, el usuario define todo a mano: el formato y cómo se escribe cada componente (año, mes, o lo que aplique) — esto ya funcionaba así, no cambia.

## 3. Preview obligatorio de tres tiempos: anterior / actual / futuro (afecta REQ-003 y REQ-004)

Al confirmar o corregir un patrón (tanto identificando un documento nuevo como editando uno existente), mostrar juntas:
- La carpeta **anterior**: real, ya existente en disco — prueba visual de que el patrón se entendió bien.
- La carpeta **actual**: ruta y nombre de archivo completos y exactos, de cómo quedaría el documento de hoy.
- La carpeta **futura**: la que se crearía cuando llegue ese período, si aplica.

Un botón para actualizar el preview en vivo a medida que se ajusta algo (ej. al corregir la marca de fecha). El objetivo es que quede clara no solo dónde queda este archivo puntual, sino cómo se van a guardar TODOS los archivos de ese tipo de ahí en adelante. Esto deja de ser opcional: es obligatorio antes de confirmar cualquier configuración nueva o editada.

## 4. Vista de un archivo ya guardado: simple, con dos botones (afecta REQ-002/REQ-004)

Fuera del momento de configurar, ver un documento ya guardado (en el historial de guardados automáticos) debe mostrar solo dónde quedó, con dos botones al lado:
- **Abrir ubicación:** abre esa carpeta en el explorador de Windows.
- **Editar configuración:** toma ESE MISMO archivo directamente — nunca pide buscar uno parecido — y lo lleva al punto de elegir carpeta del flujo normal de configuración (reusa el asistente de identificación, con el preview del punto 3).

El asistente permite retroceder un paso, pero nunca saltar hacia adelante — sigue siendo estrictamente paso a paso, como ya está especificado. Además, tiene que poder borrarse/administrarse una configuración de documento completa desde la administración de clasificaciones — si esto ya existe, confirmalo en `ESTADO.md` y no hace falta tocarlo.

## 5. "Abrir después de guardar" — reemplaza la apertura automática de PDFCreator (afecta REQ-002/REQ-003)

En el entorno real de Javier, cierto tipo de documento generado desde su ERP se abría solo en una ventana justo después de crearse, por una configuración de PDFCreator. Como Archivero ahora mueve el archivo casi al instante, esa apertura automática falla ("no lo encuentra" / "se movió"). Se agrega, por tipo de documento configurado, una opción "abrir después de guardar": si está activada, al clasificar y guardar automáticamente un archivo de ese tipo, Archivero lo abre en el visor de PDF por defecto del sistema, en su ubicación nueva.

Reglas de esa opción:
- Arranca siempre en "No" — nunca activada por defecto.
- Se activa con un solo click.
- Se elige en el último paso del asistente de configuración, no en el medio.

*(Dato de contexto, no requiere integración con PDFCreator en sí — Archivero solo tiene que replicar el efecto de "abrirlo después de guardar" con sus propios medios.)*
