using System.Runtime.CompilerServices;

// DarkTower.Data compiles everything under this folder not claimed by a nested .asmdef — i.e. the
// Definitions/Records in Data/ *and* the runtime Models/Factory (see Documentation/AssemblyArchitecture.md
// for the current-vs-recommended assembly split). Model write methods are `internal` so a presentation
// layer can only read session state; these two grants let the actual mutators — gameplay services and
// EditMode tests — reach them without widening the public API.
[assembly: InternalsVisibleTo("DarkTower.Gameplay")]
[assembly: InternalsVisibleTo("DarkTower.Gameplay.Tests")]
