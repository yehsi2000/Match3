using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    [SerializeField]
    private bool isSFX;
    private Slider slider;

    void Awake() {
        slider = this.GetComponent<Slider>();
        if (isSFX) {
            slider.value = PlayerPrefs.GetFloat("sfx_volume", 0.3f);
        } else {
            slider.value = PlayerPrefs.GetFloat("volume", 0.3f);
        }
        slider.onValueChanged.AddListener((value) => {
            if (isSFX) {
                PlayerPrefs.SetFloat("sfx_volume", value);
            } else {
                PlayerPrefs.SetFloat("volume", value);
            }
        });
    }

    // Start is called before the first frame update
    public void ToggleVisibility() {
        gameObject.SetActive(!gameObject.activeSelf);
        if (gameObject.activeSelf) {
            if (isSFX) {
                slider.value = PlayerPrefs.GetFloat("sfx_volume", 0.3f);
            } else {
                slider.value = PlayerPrefs.GetFloat("volume", 0.3f);
            }
        }
    }

    private void Start() {
        gameObject.SetActive(false);
    }
}
