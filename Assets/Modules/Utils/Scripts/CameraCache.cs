using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraCache : MonoBehaviour
{
    public static Camera Main { get; private set; }

    void Awake()
    {
        RefreshMainCamera();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnValidate()
    {
        // Editor convenience: keep cached reference up-to-date when values change in the inspector
        RefreshMainCamera();
    }

    void Update()
    {
        // Wenn die gecachte Kamera fehlt oder deaktiviert wurde, versuchen wir sie neu zu ermitteln.
        if (Main == null || !Main.isActiveAndEnabled)
        {
            RefreshMainCamera();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Szenewechsel kann die MainCamera verändern -> neu ermitteln
        RefreshMainCamera();
    }

    /// <summary>
    /// Versucht die Szene's MainCamera zu ermitteln und im Cache zu speichern.
    /// Kann statisch von anderen Klassen aufgerufen werden, falls ein sofortiges Refresh gewünscht ist.
    /// </summary>
    public static void RefreshMainCamera()
    {
        // Bevorzugt Unitys schnelle API
        var cam = Camera.main;
        if (cam != null && cam.isActiveAndEnabled)
        {
            Main = cam;
            return;
        }

        // Fallback: suche alle Kameras und wähle jene mit Tag "MainCamera"
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cameras)
        {
            if (c != null && c.CompareTag("MainCamera") && c.isActiveAndEnabled)
            {
                Main = c;
                return;
            }
        }

        // Keine gefunden -> Cache leeren
        Main = null;
    }
}
