# Caso-8 — Indicador de tiempo humano ahorrado (2026-09-22)

*Resuelto en el vault del Método JA. Funcionalidad nueva, autocontenida.*

## Qué hacer

En la ventana principal (`MainWindow`), abajo del todo (una franja tipo barra de estado, chica pero visible — no un panel grande ni algo que compita con las listas de arriba), mostrar un indicador acumulado de tiempo humano ahorrado.

## Cómo se calcula

- **X** = cantidad total histórica de documentos guardados automáticamente. Esta información ya existe: es la cantidad de filas en `GuardadoRecienteRepository` (el guardado automático de REQ-002 — no cuenta los guardados del flujo de PDFs sin texto extraíble de Caso-4, que sí requieren intervención manual del usuario y no representan el mismo ahorro).
- **Y** = X × 3 segundos, convertido a minutos (antes se había hablado de 4 segundos; el número final decidido es **3**).
- Es un total histórico acumulado desde que el usuario activó Archivero por primera vez — no una ventana de tiempo (no hay "esta semana" ni desglose por tipo, solo el total).
- La constante de 3 segundos por documento va declarada en un solo lugar del código, fácil de encontrar (una constante con nombre claro, sin necesidad de UI para cambiarla).

## Por qué se calcula así (para que quede claro el criterio del cálculo, no es un número al azar)

No se está comparando "rápido vs. lento" — el procesamiento del sistema y el proceso manual toman tiempos similares en segundos. Se está comparando tiempo humano activo requerido vs. cero intervención humana: cuando el sistema archiva solo, el usuario no gasta esos segundos de atención en absoluto (copiar/limpiar/pegar el nombre del archivo, moverlo a mano a su carpeta destino), y ese ahorro se acumula con el volumen de documentos procesados.

## Texto a mostrar (propuesta, ajustable)

Algo en la línea de:

> **X documentos archivados automáticamente — tiempo humano ahorrado: ~Y minutos**
> *(estimado a ~3 segundos de atención manual ahorrados por documento)*

La segunda línea (la explicación del criterio) va en letra más chica, como aclaración — el objetivo es que quien lo lea entienda de dónde sale el número, no que parezca inventado.

## Formato de Y cuando el número crece mucho *(sugerencia, no pedido explícito — ajustar si no gusta)*

Si el total supera los 60 minutos, conviene mostrarlo en horas y minutos (ej. "2 horas y 15 minutos") en vez de un número grande y poco legible tipo "135 minutos". Si esto complica demasiado la implementación, mostrar siempre en minutos también es aceptable — es una mejora de legibilidad, no un requisito estricto.

## Detalles de cálculo (real time vs. cacheado)

No hace falta mantener nada en memoria ni cachear: X es un simple `COUNT` sobre `GuardadoRecienteRepository`, una consulta liviana — se puede recalcular cada vez que se abre/actualiza la ventana principal sin impacto de rendimiento perceptible.
