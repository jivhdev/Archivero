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
