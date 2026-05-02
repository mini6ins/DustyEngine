namespace Utils;

public static class IdGenerator
{
    private static uint _nextGameObjectId = 1;
    private static uint _nextComponentId = 1;
    public static uint GenerateGameObjectId() => _nextGameObjectId++;
    public static uint GenerateComponentId() => _nextComponentId++;
}