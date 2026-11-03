using System.Runtime.CompilerServices;

// Model write methods are `internal` so a presentation layer can only read session state; these
// two grants let the actual mutators — gameplay services and EditMode tests — reach them without
// widening the public API.
[assembly: InternalsVisibleTo("DarkTower.Gameplay")]
[assembly: InternalsVisibleTo("DarkTower.Gameplay.Tests")]
