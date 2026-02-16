using UnityEngine;
using UnityEngine.UI;

namespace Minifantasy
{
    /// <summary>
    /// Simple UI stamina bar that reads from the PlayerMovement2D component.
    /// Attach to a UI Slider or use with a fill Image.
    /// </summary>
    public class StaminaBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private float lowThreshold = 0.25f;

        private PlayerMovement2D player;
        private CanvasGroup canvasGroup;

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.GetComponent<PlayerMovement2D>();

            canvasGroup = GetComponent<CanvasGroup>();

            EnsureRectangularFill();
        }

        private void EnsureRectangularFill()
        {
            if (fillImage == null) return;

            Texture2D whiteTex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            whiteTex.SetPixels(pixels);
            whiteTex.Apply();

            fillImage.sprite = Sprite.Create(
                whiteTex,
                new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f),
                4
            );
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
        }

        private void Update()
        {
            if (player == null || fillImage == null) return;

            float ratio = player.CurrentStamina / player.MaxStamina;
            fillImage.fillAmount = ratio;

            fillImage.color = ratio <= lowThreshold
                ? Color.Lerp(lowColor, normalColor, ratio / lowThreshold)
                : normalColor;

            if (canvasGroup != null)
            {
                float targetAlpha = (ratio >= 1f && !player.IsSprinting) ? 0f : 1f;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, 5f * Time.deltaTime);
            }
        }
    }
}
