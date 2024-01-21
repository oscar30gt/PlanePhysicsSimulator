using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttackAngleChart : MonoBehaviour
{
    [SerializeField] private Image gradient;
    [SerializeField] private Image bar;

    public void SetAngle(Vector3 dir)
    {
        float angle = Vector3.Angle(new Vector3(dir.x, 0, dir.z), dir);

        gradient.fillAmount = angle / 90;
        bar.transform.eulerAngles = new Vector3(0, 0, angle);
    }
}
