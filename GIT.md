# GitHub — comandos de uso diario

*(Referencia para el agente de IA — ver LEEME.md sobre quién ejecuta esto.)*

## Primera sesión — armar el repositorio
1. Ya parado en `C:\Archivero` (con el contenido de la semilla ya pegado):
   ```bash
   git init
   git add .
   git commit -m "feat: primer commit, semilla del Metodo JA"
   ```
2. Crear el repo en GitHub. Con GitHub CLI ya autenticado (`gh auth status`):
   ```bash
   gh repo create Archivero --private --source=. --remote=origin --push
   ```
   Sin `gh`, pedirle a Javier que cree el repo vacío en github.com y pasar la URL:
   ```bash
   git remote add origin https://github.com/USUARIO/Archivero.git
   git branch -M main
   git push -u origin main
   ```

## Retomar el proyecto en otro computador
```bash
git clone https://github.com/USUARIO/Archivero.git
```

## Flujo de todos los días
```bash
git pull
git checkout -b feature/algo-corto
# ... trabajar ...
git add -A
git commit -m "feat: qué y por qué"
git push -u origin feature/algo-corto
gh pr create --fill
```

## Si algo se traba
```bash
git status
git diff
```

## Generar el ejecutable autocontenido (empaquetado)
Para uso diario durante el desarrollo, correr directo con `dotnet run` (mucho más rápido).
Empaquetar solo en hitos reales — el resultado es un único `.exe` de Windows de ~160 MB
(incluye el runtime de .NET y la librería nativa de PDFium: no necesita tener .NET instalado
para correr) que **nunca se sube al repo** (supera el límite de tamaño de archivo de GitHub;
ver `.gitignore`, carpeta `publish/`).

```bash
dotnet publish src/Archivero/Archivero.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

El `.exe` queda en `publish/Archivero.exe`. Para distribuirlo, copiar ese archivo (y listo,
no hace falta nada más de esa carpeta salvo el `LICENSE` de PDFium que se genera al lado,
requerido por su licencia de redistribución).
