using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement(id, typeof(SceneView))]
public class NewSceneToolbarButton : EditorToolbarButton
{
    public const string id = "foundation/new-scene-button";

    public NewSceneToolbarButton()
    {
        text = "New Scene";
        clicked += ShowMenu;
    }

    private void ShowMenu()
    {
        Debug.Log("Open scene creation menu");
    }
}   



public class FoundationToolbar : ToolbarOverlay
{
    public FoundationToolbar() : base(
        NewSceneToolbarButton.id
    ) {}
}