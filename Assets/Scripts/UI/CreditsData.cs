namespace BreakingPizza.UI
{
    // Textos de los créditos finales.
    public static class CreditsData
    {
        public struct Role
        {
            public string Area;
            public string[] People;

            public Role(string area, params string[] people)
            {
                Area = area;
                People = people;
            }
        }

        public const string Tagline = "Una parodia de Breaking Bad";
        public const string Group = "GRUPO 6";

        public const string Subject = "Creatividad y Desarrollo de Entornos Virtuales";
        public const string Faculty = "UTN  ·  Facultad Regional Córdoba  ·  Curso 5K4";

        public static readonly string[] Professors =
        {
            "Ing. Inés Gimenez",
            "Ing. Franco Mana",
            "Ing. Bruno Aresu Barella",
        };

        public static readonly Role[] Team =
        {
            new Role("LIDERAZGO Y DIRECCIÓN", "Julieta Guzmán"),
            new Role("DISEÑO DE EXPERIENCIA E INTERACCIÓN", "Lourdes Osella Huber", "Arian Ocampo Vysin"),
            new Role("PROGRAMACIÓN Y DESARROLLO TÉCNICO", "Juan Cruz Patiño", "Matías Palomeque Galindo"),
            new Role("ARTE Y DISEÑO SONORO", "Carolina Jaureguialzo"),
            new Role("DISEÑO NARRATIVO Y GUIÓN", "Abel Castillo", "Bautista Mandrilli"),
        };

        // (personaje, descripción)
        public static readonly string[,] Cast =
        {
            { "Vos", "El que cocinó la pizza que salvó (o no) a Jesse" },
            { "Walter White", "El profesor de química que te metió en todo esto" },
            { "Jesse Pinkman", "El rehén, y la razón de todo" },
            { "Los secuestradores", "Muy exigentes con la pureza" },
        };

        // Modelos de Sketchfab con licencia CC BY 4.0 (la licencia OBLIGA a
        // dar crédito al autor). (título, autor)
        public static readonly string[,] Models =
        {
            { "Breaking Bad 1986 Fleetwood Bounder RV", "VectorMan" },
            { "Kitchen And Lab by Amogusstrikesback2", "@sanyabeast" },
            { "Breaking Bad Hazmat Mask Style", "FAKlR" },
            { "Walter White Lab Suit, Bryan Cranston", "00amza" },
            { "LosPollosHermanos bucket", "Batuhan13" },
        };

        public const string ModelsLicense = "Modelos de Sketchfab bajo licencia Creative Commons BY 4.0";
        public const string Environment = "Low Poly Desert Environment - Tiny Teacup Studio";

        public const string Inspiration =
            "Breaking Bad, creada por Vince Gilligan (AMC, 2008-2013).\n" +
            "Parodia sin fines de lucro, realizada con fines académicos.";

        public const string Tools = "Unity 6  ·  Universal Render Pipeline  ·  Input System  ·  TextMeshPro  ·  glTFast";

        public const string SynthMusic = "Música y efectos de sonido sintetizados por código";

        // Canción del video de créditos (Resources/Credits/creditos.mp4), como
        // "Nombre del tema - Artista". Vacío = no se muestra.
        public const string CreditsSong = "Breaking Bad: End Credits Theme - Dave Porter";
        public const string CreditsFootage = "Montaje de cierre con escenas de Breaking Bad (AMC)";

        public const string ThanksTitle = "GRACIAS POR JUGAR";

        public const string EndingWin = "Jesse volvió a casa.\nDicen que en Albuquerque todavía se habla de esa pizza.";
        public const string EndingLose = "Jesse sigue esperando...\npero siempre hay otra receta.";
        public const string EndingNeutral = "Toda historia empieza con una receta.";

        public const string FinalQuote = "\"Yeah, Mr. White! Yeah, ciencia!\"";
    }
}
