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
        new Color(255, 0, 0),     //red
        new Color(21, 32, 255),   //blue
        new Color(255, 255, 0),   //yellow
        new Color(255, 128, 0),   //orange
        new Color(207, 0, 207),   //magenta
        new Color(0, 141, 0),     //green
        new Color(255, 120, 207), //pink
        new Color(167, 255, 0),   //lime
        new Color(0, 255, 255),   //cyan
        new Color(255, 255, 255)  //white
    };
    static string[] colorNames = {"Red", "Blue", "Yellow", "Orange", "Magneta", "Green", "Pink", "Lime", "Cyan", "White"}; //to get colorblind text, just use the first letter
    static string[] traitNames = {"Boss", "Cruel", "Faulty", "Lookalike", "Puzzle", "Simon", "Time-Based", "Translated"};
    private bool[] edgework = {false, false, false, false, false, false, false, false};
    private int attempts = 1;
    private int limit = 100000;
    private bool generationValid = false;
    private int[] tra = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private int[] col = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private bool[][] chart = new bool[][] {
        new bool[] {  true,  true, false, false, false,  true, false,  true }, //red
        new bool[] {  true, false, false,  true,  true,  true, false, false }, //blue
        new bool[] { false,  true,  true,  true, false,  true, false, false }, //yellow
        new bool[] { false,  true, false, false,  true, false,  true,  true }, //orange
        new bool[] {  true, false,  true,  true,  true, false, false, false }, //magenta
        new bool[] { false,  true, false,  true, false,  true,  true, false }, //green
        new bool[] { false,  true,  true, false, false, false,  true,  true }, //pink
        new bool[] {  true, false,  true, false, false, false,  true,  true }, //lime
        new bool[] { false, false,  true,  true,  true, false, false,  true }, //cyan
        new bool[] {  true, false, false, false,  true,  true,  true, false }  //white
	  			   //boss   cruel  faulty look   puzzle  simon time    trans
    };
    private bool[][] combo = new bool[][] {
        new bool[] { false, false, false, false, false }, //trait 1
        new bool[] { false, false, false, false, false }, //trait 2
        new bool[] { false, false, false, false, false }, //etc
        new bool[] { false, false, false, false, false },
        new bool[] { false, false, false, false, false },
        new bool[] { false, false, false, false, false },
        new bool[] { false, false, false, false, false },
        new bool[] { false, false, false, false, false }
                   //color1 color2 etc
    };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { ScreenPress(); return false; };
        //ledMats[0].SetColor("_Color", colorList[0]);
    }
    void Start()
    {
        edgework[0] = (Bomb.GetOnIndicators().Count() == Bomb.GetOffIndicators().Count());    //boss
        edgework[1] = (Bomb.GetPortPlates().Any(x => x.Length == 0));                         //cruel
        edgework[2] = (Bomb.IsPortPresent(Port.Parallel) || Bomb.IsPortPresent(Port.Serial)); //faulty
        edgework[3] = (Bomb.GetBatteryCount(Battery.D) == 0);                                 //lookalike
        edgework[4] = (Bomb.GetSerialNumberNumbers().Last() % 2 == 0);                        //puzzle
        edgework[5] = (Bomb.GetBatteryCount() > 1);                                           //simon
        edgework[6] = (Bomb.GetBatteryCount() % 2 == 1);                                      //timebased
        edgework[7] = (Bomb.GetSerialNumber().Any(ch => "AEIOU".Contains(ch)));               //translated


        Debug.LogFormat("<Simon Supports #{0}> Edgework conditions: {1} {2} {3} {4} {5} {6} {7} {8}", moduleId, edgework[0], edgework[1], edgework[2], edgework[3], edgework[4], edgework[5], edgework[6], edgework[7]);

        GeneratePuzzle();

        Debug.LogFormat("[Simon Supports #{0}] Chosen colors: {1}, {2}, {3}, {4}, {5}", moduleId, colorNames[col[0]], colorNames[col[1]], colorNames[col[2]], colorNames[col[3]], colorNames[col[4]]);
        Debug.LogFormat("[Simon Supports #{0}] Chosen traits: {1}, {2}, {3}", moduleId, traitNames[tra[0]], traitNames[tra[1]], traitNames[tra[2]]);
        Debug.LogFormat("[Simon Supports #{0}] {1}'s responses: {2}, {3}, {4}", moduleId, colorNames[col[0]], FlashLog(combo[0][0]), FlashLog(combo[1][0]), FlashLog(combo[2][0]));
        Debug.LogFormat("[Simon Supports #{0}] {1}'s responses: {2}, {3}, {4}", moduleId, colorNames[col[1]], FlashLog(combo[0][1]), FlashLog(combo[1][1]), FlashLog(combo[2][1]));
        Debug.LogFormat("[Simon Supports #{0}] {1}'s responses: {2}, {3}, {4}", moduleId, colorNames[col[2]], FlashLog(combo[0][2]), FlashLog(combo[1][2]), FlashLog(combo[2][2]));
        Debug.LogFormat("[Simon Supports #{0}] {1}'s responses: {2}, {3}, {4}", moduleId, colorNames[col[3]], FlashLog(combo[0][3]), FlashLog(combo[1][3]), FlashLog(combo[2][3]));
        Debug.LogFormat("[Simon Supports #{0}] {1}'s responses: {2}, {3}, {4}", moduleId, colorNames[col[4]], FlashLog(combo[0][4]), FlashLog(combo[1][4]), FlashLog(combo[2][4]));

        Debug.LogFormat("<Simon Supports #{0}> Number of attempts: {1}", moduleId, attempts);
    }

    void GeneratePuzzle() {
        Restart:
        generationValid = true;
        tra.Shuffle(); //first 3 here are the chosen traits
        col.Shuffle(); //first 5 here are the chosen colors

        for (int i = 0; i < 40; i++) {
            combo[i%8][i/8] = chart[col[i/8]][tra[i%8]];
        }

        Check();

        if (attempts > limit) {
            Debug.LogFormat("[Simon Supports #{0}] Number of generation attempts surpassed the limit ({1}), going to unicorn state...", moduleId, limit);
            //UNICORN
        } else if (generationValid) {
            //ALL GOOD
        } else {
            attempts += 1;
            goto Restart;
        }
    }

    void Check () {
        int[] x = { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2 };
        int[] y = { 1, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 3, 4, 5, 6, 7 };

        for (int z = 0; z < x.Count(); z++) {
            if (Same(x[z], y[z])) { generationValid = false; };
        }
    }

    bool Same (int a, int b) {
        return (combo[a][0] == combo[b][0] && combo[a][1] == combo[b][1] && combo[a][2] == combo[b][2] && combo[a][3] == combo[b][3] && combo[a][4] == combo[b][4]);
    }

    string FlashLog (bool b) {
        return (b) ? "ON" : "OFF";
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
