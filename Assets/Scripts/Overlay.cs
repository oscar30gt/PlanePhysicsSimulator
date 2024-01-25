using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Overlay : MonoBehaviour
{
    [SerializeField] private Transform rightArrow;
    [SerializeField] private Transform leftArrow;
    [Space]
    [SerializeField] private Image rightBarFill;
    [SerializeField] private Image leftBarFill;

    private RectTransform rectTransform;


    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        UpdateDragBar(0);
        UpdateSpeedBar(0);
        UpdateThrustChart(0);
        UpdateTiltChart(0);
    }

    public void UpdateSpeedBar(float thrust)
    {
        rightBarFill.fillAmount = thrust;
    }

    public void UpdateDragBar(float drag)
    {
        leftBarFill.fillAmount = drag;
    }

    public void UpdateThrustChart(float thrust)
    {
        rightArrow.transform.localPosition = new(0, rectTransform.sizeDelta.y * thrust - rectTransform.sizeDelta.y / 2 , 0);
    }

    public void UpdateTiltChart(float inclination)
    {
        leftArrow.transform.localPosition = new(0, rectTransform.sizeDelta.y * inclination - rectTransform.sizeDelta.y / 2, 0);
    }
}
