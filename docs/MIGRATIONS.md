# Database Migrations Guide

Guide pour gérer les migrations EF Core et synchroniser les bases de données entre développeurs.

---

## 🎯 Concept

```
GitHub (Migrations versionnées)
    ↓ git pull
Développeur 1 & Développeur 2
    ↓ dotnet ef database update
PostgreSQL Local (Identique)
```

Les migrations sont des **fichiers C#** versionnés qui décrivent chaque changement de schéma.

---

## 📝 Workflow typique

### Scénario: Ajouter un champ à la table Users

#### 1️⃣ Développeur 1: Modifier le modèle

File: `src/Backend/MonProfAI.Domain/Entities/User.cs`

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    // ... autres champs
    public string PhoneNumber { get; set; } = null!;  // ← NOUVEAU CHAMP
    public DateTime? LastLoginAt { get; set; }  // ← NOUVEAU CHAMP
}
```

#### 2️⃣ Créer la migration

```bash
cd src/Backend

# Crée une nouvelle migration
dotnet ef migrations add AddPhoneNumberAndLastLoginToUser \
  -p MonProfAI.Infrastructure \
  -s MonProfAI.Api
```

**Le fichier créé ressemble à:**

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "PhoneNumber",
        table: "Users",
        type: "text",
        nullable: false,
        defaultValue: "");

    migrationBuilder.AddColumn<DateTime>(
        name: "LastLoginAt",
        table: "Users",
        type: "timestamp with time zone",
        nullable: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "PhoneNumber", table: "Users");
    migrationBuilder.DropColumn(name: "LastLoginAt", table: "Users");
}
```

#### 3️⃣ Appliquer localement et tester

```bash
# Appliquer la migration à votre BD locale
cd src/Backend/MonProfAI.Api
dotnet ef database update

# Tester l'API
dotnet run

# Vérifier en BD:
psql -h localhost -U postgres -d monprofai
\d "Users"  -- Voir les colonnes
```

#### 4️⃣ Committer et pousser

```bash
git add src/Backend/MonProfAI.Infrastructure/Migrations/
git commit -m "feat: add PhoneNumber and LastLoginAt to User entity"
git push origin main
```

#### 5️⃣ Développeur 2: Récupérer et appliquer

```bash
git pull  # Récupère la nouvelle migration

# Option A: Script auto
cd src/Backend
./apply-migrations.sh      # Mac/Linux
.\apply-migrations.ps1     # Windows

# Option B: Manuel
cd src/Backend/MonProfAI.Api
dotnet ef database update
```

**Boom!** Les deux bases sont identiques ✨

---

## 🛠️ Commandes courantes

### Créer une migration
```bash
cd src/Backend

# Avec nom descriptif
dotnet ef migrations add AddPhoneNumberToUser \
  -p MonProfAI.Infrastructure \
  -s MonProfAI.Api

# Sans changement détecté? Forcer quand même
dotnet ef migrations add EmptyMigration \
  -p MonProfAI.Infrastructure \
  -s MonProfAI.Api \
  --empty
```

### Appliquer les migrations
```bash
cd src/Backend/MonProfAI.Api

# Appliquer toutes les migrations en attente
dotnet ef database update

# Appliquer jusqu'à une migration spécifique
dotnet ef database update AddPhoneNumberToUser

# Voir le statut des migrations
dotnet ef migrations list
```

### Annuler une migration (avant de pousser)

```bash
cd src/Backend

# Revenir à la migration précédente
dotnet ef migrations remove

# Revenir à une migration spécifique
dotnet ef database update PreviousMigrationName
```

### Voir le SQL généré
```bash
cd src/Backend

# Afficher le SQL d'une migration
dotnet ef migrations script AddPhoneNumberToUser

# Afficher tout le SQL depuis le début
dotnet ef migrations script
```

### Inspecter la BD
```bash
# Se connecter à PostgreSQL
psql -h localhost -U postgres -d monprofai

# Voir toutes les tables
\dt

# Voir une table
\d "Users"

# Voir l'historique des migrations
SELECT * FROM "__EFMigrationsHistory";

# Quitter
\q
```

---

## 📁 Structure des migrations

```
src/Backend/MonProfAI.Infrastructure/Migrations/
├── 20241026120000_InitialSchema.cs          # La migration
├── 20241026120000_InitialSchema.Designer.cs # Metadata
├── 20241026121500_AddPhoneNumberToUser.cs
├── 20241026121500_AddPhoneNumberToUser.Designer.cs
└── MonProfAIDbContextModelSnapshot.cs        # État actuel
```

**Ne pas éditer à la main!** EF Core génère tout automatiquement.

---

## ⚠️ Bonnes pratiques

### ✅ Faire

1. **Créer une migration pour chaque changement logique**
   ```bash
   dotnet ef migrations add AddPhoneNumberToUser
   ```

2. **Tester localement avant de pousser**
   ```bash
   dotnet ef database update
   dotnet run
   ```

3. **Utiliser des noms descriptifs**
   ```bash
   # Bon
   dotnet ef migrations add AddPhoneNumberToUser
   
   # Mauvais
   dotnet ef migrations add Update1
   ```

4. **Faire un commit par migration**
   ```bash
   git commit -m "feat: add PhoneNumber to User"
   ```

5. **Pousser les migrations avant le code**
   ```bash
   git push origin main  # Les migrations d'abord
   ```

### ❌ Ne pas faire

1. **Ne pas éditer les fichiers de migration manuellement**
   - EF Core les regenere

2. **Ne pas laisser des migrations unapplied**
   - Chacun doit faire `dotnet ef database update`

3. **Ne pas modifier un modèle sans créer de migration**
   - Les deux bases deviennent out of sync

4. **Ne pas réecrire l'historique** (`git push --force`)
   - Les autres ne peuvent pas appliquer les migrations

---

## 🔄 Conflit de migrations

### Scénario: Deux développeurs créent des migrations en même temps

Dev 1 crée `AddPhoneNumber.cs`
Dev 2 crée `AddAddress.cs`

Quand Dev 2 `git pull`:
```
Conflit dans MonProfAIDbContextModelSnapshot.cs
```

**Solution:**

```bash
# Rebase plutôt que merge
git pull --rebase

# Recréer la migration
cd src/Backend
dotnet ef migrations remove  # Enlever la vôtre

# Prendre la migration de Dev 1
git checkout --theirs src/Backend/MonProfAI.Infrastructure/Migrations/

# Recréer la vôtre par-dessus
dotnet ef migrations add AddAddress -p MonProfAI.Infrastructure -s MonProfAI.Api

# Commit
git add .
git commit -m "refactor: rebase migrations after conflict"
git push
```

---

## 🚀 Premier démarrage

### Dev 1 (Crée la première migration)

```bash
cd src/Backend

# Le modèle Domain existe déjà
# Créer la migration
dotnet ef migrations add InitialSchema \
  -p MonProfAI.Infrastructure \
  -s MonProfAI.Api

# Appliquer
dotnet ef database update

# Tester
dotnet run

# Pousser
git add MonProfAI.Infrastructure/Migrations/
git commit -m "chore: create initial database schema"
git push origin main
```

### Dev 2 (Récupère et applique)

```bash
git pull

# Appliquer la migration
cd src/Backend/MonProfAI.Api
dotnet ef database update

# Vérifier
curl http://localhost:5000/api/test/users
# ✅ 200 OK - Base synchronisée!
```

---

## 📊 Checklist: Avant de pousser une migration

- [ ] Modèle Domain modifié
- [ ] Migration créée: `dotnet ef migrations add <name>`
- [ ] Migration appliquée localement: `dotnet ef database update`
- [ ] Testé manuellement en BD: `psql` ou `curl /api/test/users`
- [ ] API encore fonctionne: `dotnet run`
- [ ] Commité et pushé

---

## 🆘 Problèmes courants

### Erreur: "No migrations pending"
```
Cela signifie: Il n'y a aucune migration à appliquer.
Solution: Vérifier avec `dotnet ef migrations list`
```

### Erreur: "The DbContext was not configured to use relational database"
```
Cela signifie: La connection string PostgreSQL manque.
Solution: Vérifier .env ou appsettings.json
```

### Erreur: "Unable to connect to database"
```bash
# Vérifier que PostgreSQL démarre
docker-compose ps

# Vérifier les logs
docker-compose logs postgres
```

### Je veux annuler ma dernière migration (avant git push)
```bash
cd src/Backend

# Revenir à la migration précédente
dotnet ef database update <PreviousMigrationName>

# Supprimer le fichier de migration
dotnet ef migrations remove

# Modifions et refaisons
# ...
dotnet ef migrations add FixedMigrationName
dotnet ef database update
```

---

## 🎓 Ressources

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core CLI Commands](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

---

**Les migrations = la source de vérité pour le schéma!** 🎯
