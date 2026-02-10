# Rapport de Sécurité - DataShare API

Ce document synthétise l'audit de sécurité des dépendances et récapitule les mécanismes de protection implémentés dans l'API DataShare.

## 1. Audit des Dépendances (SCA)

L'analyse a été effectuée sur les packages NuGet (dépendances directes et transitives) pour détecter des vulnérabilités connues (CVE).

### Outil utilisé
*   **Outil :** .NET CLI (`dotnet list package`)
*   **Source des vulnérabilités :** NuGet.org / GitHub Advisory Database
*   **Date de l'audit :** 10/02/2026

### Commande exécutée
```bash
dotnet list package --vulnerable --include-transitive
