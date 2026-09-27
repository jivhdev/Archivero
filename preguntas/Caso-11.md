# Caso-11 — Seis ajustes de uso real (2026-09-26)

*Resuelto en el vault del Método JA. Sobre el Archivero standalone (no la variante en integración con Motores). Los 6 puntos son mayormente independientes entre sí — se pueden hacer en cualquier orden, uno a la vez.*

## 1. Nombre a confirmar cada vez, aunque la carpeta sea automática

**Qué agregar:** al configurar un tipo de documento (asistente de identificación/edición), agregar una opción para el campo de nombre: en vez de "extraer un campo marcado" o "mantener el nombre original" (ya existentes), una tercera alternativa **"Preguntar el nombre cada vez"**. Con esta opción activa, el documento se sigue clasificando y guardando en su carpeta de destino de forma automática como siempre (Emisor+Tipo, carpeta, formato — todo eso no cambia), pero antes de finalizar el guardado automático, el sistema muestra el documento y pide que el usuario escriba o confirme el nombre con el que se va a guardar. Se define una sola vez, al configurar ese tipo de documento (no es algo que se pregunte por fuera de la configuración).

## 2. Zoom bloqueado en "comparar documentos" (vista lado a lado de duplicados)

**Problema:** en la pantalla de comparación lado a lado (`ResolverDuplicadoWindow`, Caso-1 punto 4), no se puede hacer zoom ni scroll en los documentos mostrados — hoy está bloqueado.

**Corrección:** agregar la misma capacidad de zoom/scroll que ya existe en el visor de PDF normal, aplicada a los dos documentos de esta pantalla. Si es razonablemente simple de implementar, que el zoom quede **sincronizado entre ambos** (acercar/alejar uno mueve el otro igual) — el objetivo de esta pantalla es comparar, y perder la sincronía dificulta eso. Si sincronizar los dos resulta complejo, aceptar zoom independiente por documento es mejor que lo de hoy (nada de zoom).

## 3. Documentos pendientes viejos no se reclasifican solos cuando se crea una configuración nueva

**Problema real:** Javier identificó un documento y quedó guardado correctamente. Pero ya había OTRO documento del mismo Emisor+Tipo esperando en "pendientes por reconocer" desde antes — ese otro no se reclasificó solo con la configuración recién creada, se quedó pendiente. Tuvo que sacarlo de la carpeta observada y volver a meterlo para forzar que Archivero lo procesara de nuevo.

**Corrección, en dos partes:**
1. **Automática (preferida):** apenas se crea o se edita una configuración (termina el asistente de identificación con éxito), Archivero debe volver a intentar la coincidencia automática contra **todos** los documentos que estén actualmente en "pendientes por reconocer" — no solo contra los archivos que lleguen de ahí en adelante. Si alguno de los pendientes ahora coincide, se clasifica y guarda solo, igual que si acabara de llegar.
2. **Manual (de todas formas, como red de seguridad):** agregar un botón visible (ej. "Reprocesar pendientes") que fuerce este mismo re-chequeo a pedido del usuario, en cualquier momento.

## 4. Atajos de guardado rápido configurables, en el flujo de "distribuir" (PDFs sin texto)

**Qué agregar:** una tercera opción en la pantalla de identificación de PDFs sin texto (Caso-4), debajo de "Crear ubicación nueva" y "Ver ubicaciones disponibles": los **atajos de guardado rápido** ya guardados por el usuario (empieza vacío la primera vez).

**Cómo se crean:** dentro del flujo de "Crear ubicación nueva", justo antes de guardar (al final, con toda la configuración de esa vez ya elegida: carpeta, formato, y regla de nombre), preguntar algo corto en la línea de **"¿Guardar esta configuración como acceso rápido?"**. Si el usuario dice que sí:
- Pedir un nombre para ese atajo.
- El sistema sugiere un nombre por defecto basado en las elecciones hechas (ej. usando el nombre de la carpeta elegida, o el Emisor/Tipo si el usuario los tipeó en algún punto del flujo) — el usuario puede aceptar la sugerencia o escribir otro nombre.
- Al confirmar, la combinación completa (carpeta, formato/patrón, regla de nombre) queda guardada como un botón de acceso rápido nuevo, visible desde ese momento en la tercera opción de la pantalla de "distribuir".

**Al usar un atajo ya creado:** un click aplica toda esa configuración guardada de una — sin repetir los pasos del asistente — dejando al usuario en el punto final de revisar/editar el nombre del archivo (igual que en el flujo manual) antes de confirmar el guardado.

*(Nombre exacto de la pregunta de confirmación y de la sección de atajos, tentativos — ajustar los textos si algo suena más natural en la implementación real.)*

## 5. Documentos dañados/corruptos deben tratarse igual que los PDFs sin texto: visibles, con ruteo manual

**Problema:** hoy, si un PDF llega dañado/corrupto (no se puede leer ni renderizar, distinto del caso de "sin texto extraíble" que ya se resolvió en Caso-4), el comportamiento actual es ignorarlo en silencio (RNF-2: Archivero descarta el intento de leerlo y sigue con los demás archivos) — pero eso lo deja invisible para el usuario, igual que el bug original de "documentos fantasma" de Caso-1.

**Corrección:** un documento que Archivero detecta como dañado/no legible debe aparecer visible y gestionable con el mismo mecanismo que los PDFs sin texto extraíble (Caso-4): en la lista de "distribuir", con las mismas opciones de ruteo manual (crear ubicación nueva / ver ubicaciones disponibles / atajos de guardado rápido del punto 4). La diferencia con un PDF sin texto es que acá ni siquiera se puede mostrar el documento en el visor embebido si está realmente corrupto — si el visor no puede abrirlo, mostrar igual la entrada en la lista con un aviso claro de "no se pudo leer este archivo", y permitir igual elegir dónde guardarlo manualmente (sin poder verlo, el usuario decide con lo que sepa del nombre/contexto del archivo).

**Nota literal de Javier, para quien lo lea — verificar si aplica a este mismo bug o es un caso relacionado distinto:** *"Recordar error de año de las NCV"* — no se aclaró más detalle sobre esta referencia; si `ESTADO.md` o el historial de Archivero ya tiene contexto sobre un error de año en Notas de Crédito de Venta, revisarlo antes de asumir que es exactamente este mismo problema.

## 6. Limpieza de nombre para PDFs sin texto: quitar ceros a la izquierda que no sean parte del valor real

**Qué agregar:** en los atajos de limpieza de nombre de Caso-4 punto 4 (ya existen "Borrar" y "dejar solo números"), sumar una opción más: quitar los ceros a la **izquierda** de un nombre puramente numérico, dejando el valor entero real. Ejemplo: `"00123"` → `"123"`. Los ceros que sí forman parte del valor (no son ceros de relleno a la izquierda) no se tocan — ejemplo: `"1200"` se queda igual, esos ceros importan.

## Cierre del caso

Una vez implementados y verificados a mano los 6 puntos, regenerar el `.exe` con `dotnet publish` (self-contained, ver comando en `GIT.md`) para dejar la distribución actualizada.
