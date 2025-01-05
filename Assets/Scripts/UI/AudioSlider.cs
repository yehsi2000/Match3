using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    [SerializeField]
    private bool isSFX;
    private RectTransform rect;

    // Start is called before the first frame update
    public void ToggleVisibility() {
        gameObject.SetActive(!gameObject.activeSelf);
        if (gameObject.activeSelf) {
            Slider slider = this.GetComponent<Slider>();
            if (isSFX) {
                slider.value = PlayerPrefs.GetFloat("sfx_volume", 1);
            } else {
                slider.value = PlayerPrefs.GetFloat("volume", 1);
            }
        }
    }

    private void Start() {
        gameObject.SetActive(false);
    }
}
