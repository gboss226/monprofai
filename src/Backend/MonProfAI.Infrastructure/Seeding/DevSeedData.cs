using MonProfAI.Domain.Entities;

namespace MonProfAI.Infrastructure.Seeding;

/// <summary>
/// Test data loaded by <see cref="DevDataSeeder"/>: the maths referential for 4ème and 3ème
/// (split following the cycle 4 "repères annuels de progression") and a few fictional students.
/// Never used outside the Development environment.
/// </summary>
public static class DevSeedData
{
    public const string SubjectCode = "maths";
    public const string SubjectName = "Mathématiques";

    /// <summary>Shared password of every seeded student. Documented in the README.</summary>
    public const string StudentPassword = "Eleve2026!";

    public sealed record NotionData(string Key, string Title, string? Description = null);

    public sealed record ChapterData(SchoolLevel Level, int Order, string Title, IReadOnlyList<NotionData> Notions);

    /// <summary>A prerequisite edge: <c>NotionKey</c> requires <c>PrerequisiteKey</c>.</summary>
    public sealed record PrerequisiteData(string NotionKey, string PrerequisiteKey);

    public sealed record MasteryData(string NotionKey, double Score, int Attempts, int DaysAgo);

    public sealed record StudentData(
        string Email,
        string FullName,
        SchoolLevel Level,
        IReadOnlyList<string> StrugglingSubjects,
        LearningGoal? Goal,
        LearningStyle? Style,
        string? Objectives,
        bool OnboardingCompleted,
        IReadOnlyList<MasteryData> Masteries);

    public static readonly IReadOnlyList<ChapterData> Chapters = new[]
    {
        // ---------- 4ème ----------
        new ChapterData(SchoolLevel.Quatrieme, 1, "Nombres relatifs", new[]
        {
            new NotionData("4e.relatifs.multiplier", "Multiplier des nombres relatifs", "Règle des signes pour un produit de deux ou plusieurs facteurs."),
            new NotionData("4e.relatifs.diviser", "Diviser des nombres relatifs", "Quotient de deux nombres relatifs et règle des signes."),
            new NotionData("4e.relatifs.priorites", "Enchaîner des opérations sur les relatifs", "Priorités opératoires avec des nombres relatifs."),
        }),
        new ChapterData(SchoolLevel.Quatrieme, 2, "Fractions", new[]
        {
            new NotionData("4e.fractions.additionner", "Additionner et soustraire des fractions", "Mise au même dénominateur dans le cas général."),
            new NotionData("4e.fractions.multiplier", "Multiplier des fractions", "Produit de deux fractions et simplification."),
            new NotionData("4e.fractions.diviser", "Diviser par une fraction", "Diviser par un nombre revient à multiplier par son inverse."),
        }),
        new ChapterData(SchoolLevel.Quatrieme, 3, "Puissances", new[]
        {
            new NotionData("4e.puissances.exposant", "Puissances d'exposant entier positif", "Notation aⁿ et calculs simples."),
            new NotionData("4e.puissances.dix", "Puissances de 10", "Exposants positifs et négatifs, préfixes de nano à giga."),
            new NotionData("4e.puissances.scientifique", "Écriture scientifique", "Écrire un nombre sous la forme a × 10ⁿ avec 1 ≤ a < 10."),
            new NotionData("4e.puissances.racine", "Carré et racine carrée", "Racine carrée d'un nombre positif, carrés parfaits."),
        }),
        new ChapterData(SchoolLevel.Quatrieme, 4, "Calcul littéral", new[]
        {
            new NotionData("4e.litteral.reduire", "Réduire une expression littérale", "Regrouper les termes semblables."),
            new NotionData("4e.litteral.developper", "Développer avec la simple distributivité", "k(a + b) = ka + kb."),
            new NotionData("4e.litteral.factoriser", "Factoriser par un facteur commun", "ka + kb = k(a + b)."),
            new NotionData("4e.litteral.double", "Développer avec la double distributivité", "(a + b)(c + d) = ac + ad + bc + bd."),
        }),
        new ChapterData(SchoolLevel.Quatrieme, 5, "Équations du premier degré", new[]
        {
            new NotionData("4e.equations.tester", "Tester si un nombre est solution", "Remplacer l'inconnue et comparer les deux membres."),
            new NotionData("4e.equations.resoudre", "Résoudre une équation du premier degré", "Équations de la forme ax + b = cx + d."),
            new NotionData("4e.equations.probleme", "Mettre un problème en équation", "Choisir l'inconnue, écrire l'équation, conclure."),
        }),
        new ChapterData(SchoolLevel.Quatrieme, 6, "Proportionnalité", new[]
        {
            new NotionData("4e.proportionnalite.quatrieme", "Calculer une quatrième proportionnelle", "Produit en croix et coefficient de proportionnalité."),
            new NotionData("4e.proportionnalite.pourcentage", "Appliquer un pourcentage", "Calculer une augmentation ou une réduction en pourcentage."),
            new NotionData("4e.proportionnalite.graphique", "Reconnaître une situation de proportionnalité sur un graphique", "Points alignés sur une droite passant par l'origine."),
        }),
        new ChapterData(SchoolLevel.Quatrieme, 7, "Théorème de Pythagore", new[]
        {
            new NotionData("4e.pythagore.longueur", "Calculer une longueur avec le théorème de Pythagore", "Dans un triangle rectangle, calculer l'hypoténuse ou un côté de l'angle droit."),
            new NotionData("4e.pythagore.reciproque", "Démontrer qu'un triangle est rectangle", "Réciproque du théorème de Pythagore."),
        }),
        new ChapterData(SchoolLevel.Quatrieme, 8, "Statistiques et probabilités", new[]
        {
            new NotionData("4e.stats.moyenne", "Calculer une moyenne pondérée"),
            new NotionData("4e.stats.mediane", "Déterminer une médiane"),
            new NotionData("4e.probas.calculer", "Calculer une probabilité", "Situations d'équiprobabilité."),
        }),

        // ---------- 3ème ----------
        new ChapterData(SchoolLevel.Troisieme, 1, "Arithmétique", new[]
        {
            new NotionData("3e.arithmetique.premiers", "Reconnaître un nombre premier", "Diviseurs, multiples et nombres premiers inférieurs à 100."),
            new NotionData("3e.arithmetique.decomposition", "Décomposer en produit de facteurs premiers"),
            new NotionData("3e.arithmetique.irreductible", "Rendre une fraction irréductible", "Simplifier à l'aide de la décomposition en facteurs premiers."),
        }),
        new ChapterData(SchoolLevel.Troisieme, 2, "Calcul littéral et identités", new[]
        {
            new NotionData("3e.litteral.identite", "Développer (a + b)(a − b)", "Identité (a + b)(a − b) = a² − b²."),
            new NotionData("3e.litteral.factoriser", "Factoriser une expression", "Facteur commun ou différence de deux carrés."),
            new NotionData("3e.litteral.demontrer", "Démontrer avec le calcul littéral", "Prouver une propriété générale sur les nombres."),
        }),
        new ChapterData(SchoolLevel.Troisieme, 3, "Équations et inéquations", new[]
        {
            new NotionData("3e.equations.produit", "Résoudre une équation produit nul", "Un produit est nul si et seulement si l'un de ses facteurs est nul."),
            new NotionData("3e.equations.carre", "Résoudre une équation x² = a"),
            new NotionData("3e.equations.inequation", "Résoudre une inéquation du premier degré"),
        }),
        new ChapterData(SchoolLevel.Troisieme, 4, "Fonctions", new[]
        {
            new NotionData("3e.fonctions.image", "Image et antécédent", "Notation f(x), lecture graphique et tableau de valeurs."),
            new NotionData("3e.fonctions.lineaire", "Fonctions linéaires", "f(x) = ax, lien avec la proportionnalité."),
            new NotionData("3e.fonctions.affine", "Fonctions affines", "f(x) = ax + b, coefficient directeur et ordonnée à l'origine."),
        }),
        new ChapterData(SchoolLevel.Troisieme, 5, "Théorème de Thalès", new[]
        {
            new NotionData("3e.thales.longueur", "Calculer une longueur avec le théorème de Thalès", "Configurations des triangles emboîtés et en papillon."),
            new NotionData("3e.thales.reciproque", "Démontrer que deux droites sont parallèles", "Réciproque du théorème de Thalès."),
        }),
        new ChapterData(SchoolLevel.Troisieme, 6, "Trigonométrie", new[]
        {
            new NotionData("3e.trigo.longueur", "Calculer une longueur avec cosinus, sinus ou tangente"),
            new NotionData("3e.trigo.angle", "Calculer un angle dans un triangle rectangle"),
        }),
        new ChapterData(SchoolLevel.Troisieme, 7, "Transformations et triangles semblables", new[]
        {
            new NotionData("3e.transformations.homothetie", "Construire l'image d'une figure par une homothétie"),
            new NotionData("3e.transformations.semblables", "Reconnaître des triangles semblables", "Angles égaux deux à deux ou longueurs proportionnelles."),
        }),
        new ChapterData(SchoolLevel.Troisieme, 8, "Probabilités et statistiques", new[]
        {
            new NotionData("3e.probas.deux-epreuves", "Probabilités d'une expérience à deux épreuves", "Arbre ou tableau à double entrée."),
            new NotionData("3e.stats.etendue", "Comparer des séries avec l'étendue et la médiane"),
        }),
    };

    public static readonly IReadOnlyList<PrerequisiteData> Prerequisites = new[]
    {
        // 4ème
        new PrerequisiteData("4e.relatifs.diviser", "4e.relatifs.multiplier"),
        new PrerequisiteData("4e.fractions.diviser", "4e.fractions.multiplier"),
        new PrerequisiteData("4e.puissances.scientifique", "4e.puissances.dix"),
        new PrerequisiteData("4e.litteral.double", "4e.litteral.developper"),
        new PrerequisiteData("4e.litteral.factoriser", "4e.litteral.developper"),
        new PrerequisiteData("4e.equations.resoudre", "4e.litteral.reduire"),
        new PrerequisiteData("4e.equations.probleme", "4e.equations.resoudre"),
        new PrerequisiteData("4e.pythagore.longueur", "4e.puissances.racine"),
        new PrerequisiteData("4e.pythagore.reciproque", "4e.pythagore.longueur"),

        // 3ème, often building on 4ème
        new PrerequisiteData("3e.arithmetique.decomposition", "3e.arithmetique.premiers"),
        new PrerequisiteData("3e.arithmetique.irreductible", "3e.arithmetique.decomposition"),
        new PrerequisiteData("3e.litteral.identite", "4e.litteral.double"),
        new PrerequisiteData("3e.litteral.factoriser", "4e.litteral.factoriser"),
        new PrerequisiteData("3e.equations.produit", "3e.litteral.factoriser"),
        new PrerequisiteData("3e.equations.produit", "4e.equations.resoudre"),
        new PrerequisiteData("3e.equations.carre", "4e.puissances.racine"),
        new PrerequisiteData("3e.equations.inequation", "4e.equations.resoudre"),
        new PrerequisiteData("3e.fonctions.lineaire", "3e.fonctions.image"),
        new PrerequisiteData("3e.fonctions.lineaire", "4e.proportionnalite.graphique"),
        new PrerequisiteData("3e.fonctions.affine", "3e.fonctions.lineaire"),
        new PrerequisiteData("3e.thales.longueur", "4e.proportionnalite.quatrieme"),
        new PrerequisiteData("3e.thales.reciproque", "3e.thales.longueur"),
        new PrerequisiteData("3e.trigo.longueur", "4e.pythagore.longueur"),
        new PrerequisiteData("3e.trigo.angle", "3e.trigo.longueur"),
        new PrerequisiteData("3e.transformations.semblables", "3e.thales.longueur"),
        new PrerequisiteData("3e.probas.deux-epreuves", "4e.probas.calculer"),
        new PrerequisiteData("3e.stats.etendue", "4e.stats.mediane"),
    };

    /// <summary>Fictional students. Emails use the reserved <c>.test</c> domain so they can never reach a real inbox.</summary>
    public static readonly IReadOnlyList<StudentData> Students = new[]
    {
        // 4ème, onboarding done, struggling with fractions.
        new StudentData(
            Email: "ines.4e@monprofai.test",
            FullName: "Inès Moreau",
            Level: SchoolLevel.Quatrieme,
            StrugglingSubjects: new[] { "Mathématiques", "Physique-Chimie" },
            Goal: LearningGoal.ImproveGrades,
            Style: LearningStyle.ConcreteExamples,
            Objectives: "Ne plus avoir peur des fractions.",
            OnboardingCompleted: true,
            Masteries: new[]
            {
                new MasteryData("4e.relatifs.multiplier", 0.80, 6, 3),
                new MasteryData("4e.relatifs.diviser", 0.65, 4, 3),
                new MasteryData("4e.fractions.additionner", 0.35, 5, 1),
                new MasteryData("4e.fractions.multiplier", 0.50, 3, 1),
                new MasteryData("4e.puissances.dix", 0.60, 2, 7),
            }),

        // 3ème preparing the brevet, solid on 4ème, discovering 3ème notions.
        new StudentData(
            Email: "hugo.3e@monprofai.test",
            FullName: "Hugo Lefèvre",
            Level: SchoolLevel.Troisieme,
            StrugglingSubjects: new[] { "Mathématiques" },
            Goal: LearningGoal.Brevet,
            Style: LearningStyle.Diagrams,
            Objectives: "Avoir au moins 15 en maths au brevet.",
            OnboardingCompleted: true,
            Masteries: new[]
            {
                new MasteryData("4e.litteral.double", 0.90, 8, 20),
                new MasteryData("4e.equations.resoudre", 0.75, 7, 15),
                new MasteryData("4e.pythagore.longueur", 0.95, 9, 30),
                new MasteryData("4e.proportionnalite.quatrieme", 0.85, 5, 25),
                new MasteryData("3e.thales.longueur", 0.60, 4, 2),
                new MasteryData("3e.fonctions.image", 0.40, 3, 1),
                new MasteryData("3e.trigo.longueur", 0.20, 2, 0),
            }),

        // Fresh account that has not filled in the onboarding questionnaire yet.
        new StudentData(
            Email: "sacha.nouveau@monprofai.test",
            FullName: "Sacha Bernard",
            Level: SchoolLevel.Sixieme,
            StrugglingSubjects: Array.Empty<string>(),
            Goal: null,
            Style: null,
            Objectives: null,
            OnboardingCompleted: false,
            Masteries: Array.Empty<MasteryData>()),
    };
}
