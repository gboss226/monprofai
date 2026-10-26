# GitHub Actions CI/CD Pipeline

Documentation du pipeline d'intégration continue et déploiement.

---

## 🔄 Workflow

### À chaque Pull Request

Le pipeline automatiquement:

1. ✅ **Build Backend** (.NET)
   - Restore les dépendances
   - Build en Release
   - Lance les migrations
   - Exécute les tests

2. ✅ **Build Frontend** (React)
   - Installe les dépendances
   - Type check (TypeScript)
   - Build l'application
   - Exécute les tests

3. ✅ **Lint & Validation**
   - Valide `docker-compose.yml`
   - Vérifie le formatage

4. ✅ **Summary** - Récapitulatif final

---

## 📊 Voir les résultats

### Sur GitHub

1. Allez sur votre Pull Request
2. Scrollez vers le bas → Section "Checks"
3. Vous verrez:
   ```
   ✅ CI - Build & Test
   ├─ Backend - Build & Test
   ├─ Frontend - Build & Lint
   ├─ Lint & Docker
   └─ Check Summary
   ```

### Détails d'un test qui échoue

Cliquez sur le test qui échoue → Voir les logs complets

---

## 🧪 Tests Backend

### Structure

```
src/Backend/MonProfAI.Tests/
├── Controllers/
│   └── HealthControllerTests.cs
├── Domain/
│   └── UserEntityTests.cs
└── MonProfAI.Tests.csproj
```

### Framework

- **xUnit** - Framework de test
- **Moq** - Mocking
- **Fact** - Tests sans paramètres
- **Theory** - Tests paramétrés

### Exemple de test

```csharp
[Fact]
public void CreateUser_WithValidData_Succeeds()
{
    // Arrange
    var user = new User { Email = "test@example.com" };

    // Act & Assert
    Assert.Equal("test@example.com", user.Email);
}
```

### Lancer les tests localement

```bash
# Tous les tests
dotnet test src/Backend/

# Tests d'un dossier
dotnet test src/Backend/MonProfAI.Tests/

# Avec verbose
dotnet test src/Backend/ -v detailed

# Voir la couverture
dotnet test src/Backend/ /p:CollectCoverage=true
```

---

## 📝 Ajouter un nouveau test

### 1. Créer le fichier

```bash
src/Backend/MonProfAI.Tests/
└── Features/
    └── AuthTests.cs
```

### 2. Écrire le test

```csharp
using Xunit;
using MonProfAI.Application.UseCases.Auth;

namespace MonProfAI.Tests.Features;

public class AuthTests
{
    [Fact]
    public void RegisterUser_WithValidEmail_Succeeds()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            FullName = "Test User",
            Password = "Password123!"
        };

        // Act & Assert
        Assert.NotNull(command);
    }
}
```

### 3. Lancer le test

```bash
dotnet test src/Backend/MonProfAI.Tests/
```

### 4. Commit et Push

```bash
git add src/Backend/MonProfAI.Tests/
git commit -m "test: add auth registration tests"
git push origin feature/new-feature
```

---

## 🎯 Tests Frontend

### Lancer les tests

```bash
cd src/Frontend
npm test
```

### Ajouter un test React

```typescript
// src/Frontend/src/components/Login.test.tsx
import { render, screen } from '@testing-library/react'
import { Login } from './Login'

describe('Login Component', () => {
  it('renders login form', () => {
    render(<Login />)
    expect(screen.getByText('Se connecter')).toBeInTheDocument()
  })
})
```

---

## 🚀 Workflow de développement

### 1. Créer une branche

```bash
git checkout -b feature/add-courses
```

### 2. Développer et tester localement

```bash
# Backend
cd src/Backend
dotnet test

# Frontend
cd src/Frontend
npm run build
npm test
```

### 3. Commit et Push

```bash
git add .
git commit -m "feat: add courses CRUD"
git push origin feature/add-courses
```

### 4. Créer une PR sur GitHub

- Allez sur https://github.com/gboss226/monprofai
- Cliquez "Compare & pull request"
- Attendez les tests CI (2-5 minutes)

### 5. Voir les résultats

```
✅ All checks passed
├─ Backend - Build & Test ✅
├─ Frontend - Build & Lint ✅
└─ Lint & Docker ✅
```

### 6. Demander une review

- Assignez un reviewer
- Attendez l'approbation

### 7. Merger la PR

- Cliquez "Squash and merge" ou "Merge"
- La branche est automatiquement supprimée

---

## ⚠️ Erreurs courantes

### Build échoue: "Package not found"

```
error NU1101: Unable to find package Microsoft.Something
```

**Solution:**
```bash
dotnet nuget update source nuget.org -s https://api.nuget.org/v3/index.json
dotnet restore
```

### Tests échoient en CI mais passent localement

**Causes possibles:**
- Différence de configuration d'environnement
- Problème de port déjà utilisé
- Timing/race condition

**Solution:**
```bash
# Nettoyer et relancer
dotnet clean
dotnet test src/Backend/
```

### Build frontend échoue

```
error TS2304: Cannot find name 'React'
```

**Solution:**
```bash
cd src/Frontend
npm install
npm run build
```

---

## 🔍 Debugger les tests CI

### Voir les logs complets

1. Cliquez sur le test qui échoue dans la PR
2. Cliquez "Show more"
3. Scrollez pour voir les logs détaillés

### Re-run un test

1. Cliquez "Re-run jobs"
2. Choisir le job qui a échoué
3. Cliquez "Re-run"

---

## 📈 Améliorer la couverture

### Coverage Report

```bash
dotnet test src/Backend/ /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Voir la couverture

```bash
reportgenerator -reports:"src/Backend/**/coverage.opencover.xml" \
  -targetdir:coverage \
  -reporttypes:Html
```

Puis ouvrir `coverage/index.html`

---

## 🎓 Bonnes pratiques

### ✅ Faire

1. **Écrire des tests pour les nouvelles features**
   ```csharp
   [Fact]
   public void CreateCourse_WithValidTitle_Succeeds()
   ```

2. **Lancer les tests localement avant de pusher**
   ```bash
   dotnet test
   ```

3. **Noms de tests explicites**
   ```
   ✅ RegisterUser_WithValidEmail_Succeeds
   ❌ Test1
   ```

4. **Un test = une chose à tester**
   ```csharp
   // Tester seulement la création, pas le save aussi
   [Fact]
   public void CreateUser_WithValidData_ReturnsUser()
   ```

5. **Arrange-Act-Assert**
   ```csharp
   // Arrange
   var user = new User { Email = "test@example.com" };
   // Act
   var result = service.Create(user);
   // Assert
   Assert.NotNull(result);
   ```

### ❌ Ne pas faire

1. **Ne pas ignorer les tests qui échouent**
   ```csharp
   [Fact(Skip = "Fix later")] // ❌ Ne pas faire
   ```

2. **Ne pas faire des tests qui dépendent d'autres tests**
   ```csharp
   // ❌ Test 2 dépend de Test 1
   // ✅ Chaque test doit être indépendant
   ```

3. **Ne pas faire des tests flaky** (parfois passent, parfois échouent)
   ```csharp
   // ❌ Dépend du timing
   Thread.Sleep(1000);
   
   // ✅ Utiliser Moq ou injecter le time provider
   ```

---

## 📚 Ressources

- [GitHub Actions Documentation](https://docs.github.com/actions)
- [xUnit Documentation](https://xunit.net/)
- [Jest (Frontend testing)](https://jestjs.io/)

---

**Les tests = confiance dans le code!** 🎯
