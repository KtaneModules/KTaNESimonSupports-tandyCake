using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class SimonSupportsScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable button;
    public Material[] ledMats;
    private Color[] colorList = new Color[]
    {
        new Color(255, 0, 0),
        new Color(21, 32, 255),
        new Color(255, 255, 0),
        new Color(255, 128, 0),
        new Color(173, 0, 191),
        new Color(0, 141, 0),
        new Color(255, 120, 207),
        new Color(167, 255, 0),
        new Color(0, 255, 255),
        new Color(255, 255, 255)
};

static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { ScreenPress(); return false; };
        ledMats[0].SetColor("_Color", colorList[0]);
    }
    void Start()
    {

    }

    void ScreenPress()
    {

    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
    }

    IEnumerator TwitchHandleForcedSolve () {
      yield return null;
    }
}
