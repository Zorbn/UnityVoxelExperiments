using System;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class DebugText : MonoBehaviour
{
    public const float UpdateTime = 0.25f;
    private TMP_Text text;
    private float updateTimer = 0f;

    private void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        updateTimer -= Time.deltaTime;

        if (updateTimer < 0f)
        {
            updateTimer = UpdateTime;
            text.text = $"FPS: {1f / Time.deltaTime}";
        }
    }
}
