using DustyEngine.Scene;

namespace DustyEngine.Core;

public static class PathUtility
{
    public static string GetRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        path = path.Replace('\\', '/');

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return path;

        if (!string.IsNullOrWhiteSpace(SceneManager.ProjectPath))
        {
            string projectPath = SceneManager.ProjectPath.Replace('\\', '/');

            if (projectPath.EndsWith('/'))
                projectPath = projectPath.TrimEnd('/');

            if (path.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = path.Substring(projectPath.Length).TrimStart('/');

                if (!relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    if (relativePath.Contains("Assets/", StringComparison.OrdinalIgnoreCase))
                    {
                        int assetsIndex = relativePath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                        relativePath = relativePath.Substring(assetsIndex);
                    }
                    else
                    {
                        relativePath = "Assets/" + relativePath;
                    }
                }

                return relativePath;
            }
        }

        return path;
    }


    public static string GetAbsolutePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        if (!string.IsNullOrWhiteSpace(SceneManager.ProjectPath))
        {
            path = path.Replace('\\', '/');

            return Path.GetFullPath(Path.Combine(SceneManager.ProjectPath, path));
        }

        return path;
    }


    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        return path.Replace('\\', '/');
    }
}
