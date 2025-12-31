using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }
    
    [Header("Coin Settings")]
    [SerializeField] private int currentCoins = 0;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Text coinTextLegacy;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        UpdateCoinUI();
    }
    
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateCoinUI();
        Debug.Log($"Coins added: {amount}. Total coins: {currentCoins}");
    }
    
    public void RemoveCoins(int amount)
    {
        currentCoins -= amount;
        if (currentCoins < 0) currentCoins = 0;
        UpdateCoinUI();
        Debug.Log($"Coins removed: {amount}. Total coins: {currentCoins}");
    }
    
    public int GetCurrentCoins()
    {
        return currentCoins;
    }
    
    private void UpdateCoinUI()
    {
        // Format: ": 10" (User wants colon separator)
        string coinString = $": {currentCoins}";
        
        if (coinText != null)
        {
            coinText.text = coinString;
        }
        
        if (coinTextLegacy != null)
        {
            coinTextLegacy.text = coinString;
        }
    }
}
