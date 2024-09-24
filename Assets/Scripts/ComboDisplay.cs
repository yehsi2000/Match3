using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ComboDisplay : MonoBehaviour
{
    TMP_Text comboText;
    float displayTime;
    float comboDisplayTime = 1f;
    // Start is called before the first frame update
    void Awake()
    {
        comboText = GetComponent<TMP_Text>();
    }

    public void Initialize(float combotime) {
        //comboText = GetComponent<TMP_Text>();
        comboText.enabled = false;
        comboDisplayTime = combotime;

    }

    // Update is called once per frame
    public void UpdateCombo(int combo)
    {
        comboText.enabled = true;
        comboText.text = combo + " Combo!";
        displayTime = comboDisplayTime;
    }

    private void Update() {
        displayTime -= Time.deltaTime;
        if (displayTime < 0f) {
            comboText.enabled = false;
        }
    }
}
