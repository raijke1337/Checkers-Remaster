using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HelperText : MonoBehaviour
{
    private Text _txt;
    private void Start()
    {
        _txt = GetComponent<Text>();
    }

    public void WriteInfo(string text)
    {
        _txt.text = text;
    }
}
