using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ComboDisplay : MonoBehaviour
{
    TMP_Text comboText;
    float displayTime;
    float comboDisplayTime = 2f;
    // Start is called before the first frame update
    void Awake()
    {
        comboText = GetComponent<TMP_Text>();
    }

    public void Initialize(float combotime) {
        comboText.enabled = false;
        comboDisplayTime = combotime/2;
    }

    // Update is called once per frame
    public void UpdateCombo(int combo)
    {
        comboText.enabled = true;
        comboText.alpha = 1f;
        comboText.text = combo + " Combo!";
        displayTime = comboDisplayTime;
    }

    private void Update() {
        displayTime -= Time.deltaTime;
        if (displayTime < 0f) {
            comboText.alpha -= Mathf.Lerp(0, 1, Time.deltaTime/comboDisplayTime);
        }
        
    }
}
