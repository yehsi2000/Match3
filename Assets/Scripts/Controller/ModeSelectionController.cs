using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModeSelectionController : MonoBehaviour
{
    [SerializeField]
    Button singleplayButton;
    [SerializeField]
    Button raidButton;
    [SerializeField]
    Button multiplayButton;

    // Start is called before the first frame update
    void Start()
    {
        singleplayButton.onClick.AddListener(() => {
            SceneManager.LoadScene("SingleGameScene");
        });

        raidButton.onClick.AddListener(() => {
            //SceneManager.LoadScene("RaidGameScene");
        });

        multiplayButton.onClick.AddListener(() => {
            SceneManager.LoadScene("WaitingRoom");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
