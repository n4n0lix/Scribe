using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Scribe.Tools.Editor {

public static class SceneSwitcherToolbar
{
    [MainToolbarElement("Scene Switcher", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement CreateSceneSwitcher()
    {
        var dropdown = new MainToolbarDropdown(
            new MainToolbarContent(GetCurrentSceneName()),
            ShowSceneMenu
        );

        EditorSceneManager.activeSceneChangedInEditMode += (_, _) =>
        {
            dropdown.content = new MainToolbarContent(GetCurrentSceneName());
        };

        return dropdown;
    }

    private static void ShowSceneMenu(Rect rect)
    {
        var menu = new GenericMenu();

        string currentPath = EditorSceneManager.GetActiveScene().path;

        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
                continue;

            string scenePath = scene.path;
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            bool isCurrent = scenePath == currentPath;

            menu.AddItem(
                new GUIContent(sceneName),
                isCurrent,
                () => OpenScene(scenePath)
            );
        }

        menu.DropDown(rect);
    }

    private static void OpenScene(string path)
    {
        if (EditorSceneManager.GetActiveScene().path == path)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path);
    }

    private static string GetCurrentSceneName()
    {
        var scene = EditorSceneManager.GetActiveScene();

        return string.IsNullOrEmpty(scene.name)
            ? "Scene"
            : scene.name;
    }
}
}