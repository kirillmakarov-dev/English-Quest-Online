using UnityEngine;

public class NotebookInputHandler : MonoBehaviour
{
    [Header("Keyboard Inputs")]

    [Tooltip("The key to open or close the notebook. Default is KeyCode.N.")]
    public KeyCode ToggleKey = KeyCode.N;

    [Tooltip("The key to go back or close the notebook while open. Default is KeyCode.Escape.")]
    public KeyCode CloseKey = KeyCode.Escape;

    [Tooltip("The primary key to navigate to the next page. Default is KeyCode.RightArrow.")]
    public KeyCode NextPageKey = KeyCode.RightArrow;

    [Tooltip("The alternate key to navigate to the next page. Default is KeyCode.D.")]
    public KeyCode NextPageAltKey = KeyCode.D;

    [Tooltip("The primary key to navigate to the previous page. Default is KeyCode.LeftArrow.")]
    public KeyCode PreviousPageKey = KeyCode.LeftArrow;

    [Tooltip("The alternate key to navigate to the previous page. Default is KeyCode.A.")]
    public KeyCode PreviousPageAltKey = KeyCode.A;

    public bool TogglePressed => Input.GetKeyDown(ToggleKey);
    public bool ClosePressed => Input.GetKeyDown(CloseKey);
    public bool NextPagePressed => Input.GetKeyDown(NextPageKey) || Input.GetKeyDown(NextPageAltKey);
    public bool PreviousPagePressed => Input.GetKeyDown(PreviousPageKey) || Input.GetKeyDown(PreviousPageAltKey);

    [ContextMenu("Set Default Inputs")]
    private void SetDefaultInputs()
    {
        ToggleKey = KeyCode.N;
        CloseKey = KeyCode.Escape;
        NextPageKey = KeyCode.RightArrow;
        NextPageAltKey = KeyCode.D;
        PreviousPageKey = KeyCode.LeftArrow;
        PreviousPageAltKey = KeyCode.A;
    }
}
