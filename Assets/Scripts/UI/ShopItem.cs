using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI boostValueText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI costText;

    public TextMeshProUGUI BoostValueText => boostValueText;
    public TextMeshProUGUI NameText => nameText;
    public TextMeshProUGUI LevelText => levelText;
    public Button BuyButton => buyButton;
    public TextMeshProUGUI CostText => costText;

    public void SetIcon(Sprite iconSprite)
    {
        if (iconImage == null)
            return;

        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    public void SetIconSize(Vector2 size)
    {
        if (iconImage == null)
            return;

        RectTransform rect = iconImage.rectTransform;
        if (rect != null)
            rect.sizeDelta = size;
    }
}
