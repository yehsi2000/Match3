using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TutorialPopup : MonoBehaviour, IDeselectHandler
{

    public void Active() {
        this.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnDeselect(BaseEventData eventData) {
        Debug.Log("Clicked Outside");
        this.gameObject.SetActive(false);
    }
}
