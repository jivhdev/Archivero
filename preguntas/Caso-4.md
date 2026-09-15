# Caso-4 — Rediseño del flujo de PDFs sin texto extraíble (2026-09-15)

*Resuelto en el vault del Método JA. Pegar este archivo en `preguntas/Caso-4.md` del repositorio de código. Ejecutar DESPUÉS de Caso-3 (este Caso reutiliza el asistente de tipo/patrón/fecha/vista previa de Caso-3, no crea uno nuevo).*

**Contexto:** el punto 1 de Caso-1 (implementado como `IdentificarSinTextoWindow`) no resultó lo que Javier necesitaba en la práctica. Este Caso lo reemplaza por completo. Sos un ejecutor, no un diseñador — seguí los pasos tal como están, sin improvisar. Si algo no está cubierto acá, detenete y avisale a Javier.

## 1. Categoría separada, propia lista

Hoy los PDF sin texto extraíble caen en la misma lista que "Pendientes por reconocer". Separarlos: crear una lista nueva y distinta (nombre tentativo: **"Pendientes de distribuir"** — el nombre final queda abierto, como otros nombres de interfaz de `SPEC.md`). Un PDF sin texto extraíble nunca aparece en "Pendientes por reconocer"; aparece únicamente en esta lista nueva.

## 2. Visor de PDF con zoom

Al hacer click en un documento de esta lista, se abre el visor de PDF embebido (el mismo componente ya usado en el resto de la app). Hoy ese visor no permite hacer zoom — agregar la posibilidad de hacer zoom sobre el PDF mostrado (acercar/alejar), tanto acá como, si aplica, en el resto de los lugares donde se usa el mismo visor.

## 3. Pantalla de identificación: dos opciones

A la izquierda del visor (igual que hoy) aparece el texto "Este documento no tiene texto que se pueda leer". Debajo, dos opciones:

- **Crear ubicación nueva**
- **Ver ubicaciones disponibles**

### 3a. "Crear ubicación nueva"

1. El programa pide elegir la **carpeta madre** de destino.
2. El programa revisa si esa carpeta ya tiene subcarpetas adentro:
   - **Si tiene subcarpetas:** el usuario configura tipo de organización, patrón, y fecha de referencia usando exactamente el mismo asistente descrito en `Caso-3` (Paso 3 completo: accesos rápidos, ver todas las opciones, elegir patrón con ejemplos, marcar fecha si aplica, vista previa obligatoria). No se reimplementa nada nuevo acá — se reutiliza el mismo componente/flujo.
   - **Si NO tiene subcarpetas:** preguntar directamente si se guarda directo en esa carpeta (equivalente al Paso 2 de Caso-3, opción "Directo").
3. La única diferencia real respecto al asistente normal de identificación (Caso-3): en este flujo, como no hay texto extraíble, la fecha del documento (si aplica al tipo elegido) la marca el usuario manualmente sobre el PDF de la misma forma que ya se hace para otros campos — no hay nada que auto-detectar.

### 3b. "Ver ubicaciones disponibles"

Muestra una lista de todas las ubicaciones ya creadas anteriormente a través de "Crear ubicación nueva" en este mismo flujo (sin texto) — **no** mezclar con las configuraciones normales de Emisor+Tipo del resto de la app, son listas separadas.

Al hacer click en una ubicación de esa lista, el comportamiento depende de cómo se configuró esa ubicación:

1. **Si se configuró "Directo en la carpeta":** mostrar una confirmación simple ("¿Guardar este documento en [ruta]?"). Si el usuario confirma, el archivo se guarda ahí.
2. **Si se configuró con algún tipo de organización (por año, por año y mes, etc.):** mostrar una navegación tipo explorador, dentro de la propia app:
   - Si es "Por año" (o cualquier tipo con año como único nivel): mostrar la lista de años ya existentes/disponibles para elegir.
   - Si es "Por año y mes" (o similar de dos niveles): mostrar primero los años, y al elegir uno, mostrar los meses dentro de ese año.
   - Mismo criterio para los demás tipos de Caso-3 que tengan más de un nivel de subcarpeta.
   - El objetivo es ahorrarle al usuario el paso de navegar manualmente por Windows Explorer — no hace falta que sea más sofisticado que una lista clickeable por nivel.

## 4. Editar el nombre del archivo, al final, antes de guardar

Justo antes de guardar (último paso, con el PDF todavía abierto y visible), el usuario puede editar el nombre con el que se va a guardar el archivo:

- El campo de nombre arranca con el **nombre de archivo original** como punto de partida (no arranca vacío) — muchos de estos documentos vienen con el número o nombre correcto ya en el nombre de archivo, solo hace falta limpiarlo.
- Agregar un botón **"Borrar"** que vacía completamente el campo de nombre, para escribir uno nuevo desde cero.
- Agregar un botón/atajo que **deje solo los números** que haya en el nombre actual (quitando letras, guiones, espacios, etc. — solo se conservan los dígitos). Útil porque muchos de estos documentos usan un número de guía/folio como identificador, mezclado con otro texto en el nombre de archivo original.

## Cierre del caso

Una vez implementado y verificado a mano, regenerar el `.exe` con `dotnet publish` (self-contained, ver comando en `GIT.md`) para dejar la distribución actualizada.
