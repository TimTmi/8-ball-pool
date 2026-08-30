using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using EightBall.UI;

public class SceneSetupTool
{
    [MenuItem("Tools/Setup Scenes")]
    public static void SetupScenes()
    {
        CreateMainMenuScene();
        CreateGameplayScene();

        // Add to build settings
        var newScenes = new EditorBuildSettingsScene[2];
        newScenes[0] = new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true);
        newScenes[1] = new EditorBuildSettingsScene("Assets/Scenes/Gameplay.unity", true);
        EditorBuildSettings.scenes = newScenes;

        Debug.Log("Scenes created and added to Build Settings successfully!");
    }

    private static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject cameraObj = new GameObject("Main Camera");
        var camera = cameraObj.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObj.tag = "MainCamera";

        GameObject uiObj = new GameObject("UI");
        var uiDoc = uiObj.AddComponent<UIDocument>();
        
        var panelSettingsGuids = AssetDatabase.FindAssets("t:PanelSettings");
        PanelSettings panelSettings;
        if (panelSettingsGuids.Length > 0)
        {
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(panelSettingsGuids[0]));
        }
        else
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panelSettings, "Assets/UI/DefaultPanelSettings.asset");
        }
        uiDoc.panelSettings = panelSettings;

        uiObj.AddComponent<MainMenuController>();
        
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenu.uxml");
        if (visualTree != null) uiDoc.visualTreeAsset = visualTree;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    private static void CreateGameplayScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject cameraObj = new GameObject("Main Camera");
        var camera = cameraObj.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObj.tag = "MainCamera";

        GameObject uiObj = new GameObject("UI");
        var uiDoc = uiObj.AddComponent<UIDocument>();
        
        var panelSettingsGuids = AssetDatabase.FindAssets("t:PanelSettings");
        PanelSettings panelSettings;
        if (panelSettingsGuids.Length > 0)
        {
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(panelSettingsGuids[0]));
        }
        else
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panelSettings, "Assets/UI/DefaultPanelSettings.asset");
        }
        uiDoc.panelSettings = panelSettings;

        uiObj.AddComponent<GameplayUIController>();
        
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameplayUI.uxml");
        if (visualTree != null) uiDoc.visualTreeAsset = visualTree;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Create Pool Table Mockup
        GameObject tableObj = new GameObject("Table");
        var tableRenderer = tableObj.AddComponent<SpriteRenderer>();
        tableObj.transform.position = Vector3.zero;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Gameplay.unity");
    }
}
