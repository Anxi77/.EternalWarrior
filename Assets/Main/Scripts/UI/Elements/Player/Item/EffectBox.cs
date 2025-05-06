using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EffectBox : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI effectNameText;

    [SerializeField]
    private TextMeshProUGUI effectDescriptionText;

    public void SetEffect(ItemEffect itemEffect)
    {
        effectNameText.text = itemEffect.effectName;
        effectDescriptionText.text = itemEffect.description;
    }
}
