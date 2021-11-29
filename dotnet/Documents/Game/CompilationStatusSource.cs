namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record CompilationStatus(
        bool isComplete,
        bool containsErrors,
        string log,
        string source
        );
}
