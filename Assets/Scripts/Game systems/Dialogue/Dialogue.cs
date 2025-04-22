using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI text_component;
    public string[] lines;
    public float text_speed;
    private int index;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text_component.text = string.Empty;
        start_dialogue();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if (text_component.text == lines[index])
            {
                next_line();
            }
            else
            {
                StopAllCoroutines();
                text_component.text = lines[index];
            }
        }
    }

    void start_dialogue()
    {
        index = 0;
        StartCoroutine(type_line());
    }

    IEnumerator type_line()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            text_component.text += c;
            yield return new WaitForSeconds(text_speed);
        }
    }
    
    void next_line()
    {
        if (index < lines.Length - 1)
        {
            index++;
            text_component.text = string.Empty;
            StartCoroutine(type_line());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
