using UnityEngine;
using UnityEngine.SceneManagement; // Wajib ada untuk mengatur perpindahan scene

public class PindahSceneMap1 : MonoBehaviour
{
    [Header("UI Popups")]
    public GameObject settingsPopup; // Slot untuk menyeret objek SettingsPopup
    public GameObject guidePopup;   // Slot untuk menyeret objek GuidePopup

    // Fungsi awal Anda untuk berpindah ke Map 1
    public void PindahKeMap1()
    {
        SceneManager.LoadScene("Maps 1");
    }

    // --- FUNGSI BARU UNTUK KONTROL POPUP ---

    // Dipanggil oleh SettingButton (Main Menu)
    public void OpenSettings()
    {
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(true);
            // Sembunyikan guide saat pertama kali buka setting agar tidak tumpuk
            if (guidePopup != null) guidePopup.SetActive(false);
        }
    }

    // Dipanggil oleh tombol "Guide" di dalam SettingsPopup
    public void OpenGuide()
    {
        if (guidePopup != null) guidePopup.SetActive(true);
    }

    // Dipanggil oleh tombol "Back" di dalam GuidePopup
    public void CloseGuide()
    {
        if (guidePopup != null) guidePopup.SetActive(false);
    }

    // Dipanggil oleh tombol "Close" (X) di dalam SettingsPopup
    public void CloseSettings()
    {
        if (settingsPopup != null) settingsPopup.SetActive(false);
    }

    public void KeluarGame()
    {
        Debug.Log("Game Keluar...");
        Application.Quit();
    }
}