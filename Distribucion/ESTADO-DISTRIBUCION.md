Estado actual: En piloto desde 2026-09-13 — pendiente de revisión.

Última actualización del .exe: 2026-09-15, con `preguntas/Caso-3.md` (rediseño guiado de formato/patrón de carpetas), `Caso-4.md` (rediseño del flujo de PDFs sin texto extraíble) y `Caso-5.md` (botón "Eliminar duplicado") — ver `ESTADO.md`. El punto 2 de Caso-2 (vigilancia que no reacciona a borrados manuales) todavía no está incluido.

## Qué contiene esta carpeta
- `Archivero.exe`: ejecutable autocontenido de Windows (no requiere .NET instalado). Se abre con doble click.
- `Archivero.pdb`: símbolos de depuración (no hace falta para ejecutarlo).
- `LICENSE`: licencia de terceros de PDFium (Docnet.Core la incluye automáticamente al publicar).

El `.exe` pesa unos 160MB y nunca se sube al repositorio (ver `.gitignore`) — supera el límite de tamaño de archivo de GitHub. Solo este archivo (`ESTADO-DISTRIBUCION.md`) queda versionado.

## Cómo se regenera
Desde la raíz del repo:

```
dotnet publish src/Archivero/Archivero.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Distribucion
```

Ver `GIT.md` para más detalle sobre este comando.
