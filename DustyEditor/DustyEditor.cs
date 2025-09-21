namespace DustyEditor;

internal static class DustyEditor
{
    private const string ProjectPath = "/home/maksym/github/DustyEngine/TestProject";

    static void Main(string[] args)
    {
        DustyEngine.DustyEngine engine = new DustyEngine.DustyEngine();

        engine.StartEngine(ProjectPath);
    }
}