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
    public KMColorblindMode CB;
    public KMSelectable button;
    public Material[] ledMats;
    public GameObject[] leds;
    public Light[] lights;
    public AudioClip[] sounds;
    public TextMesh[] cbTexts;

    static string[] colorNames = {"Red", "Blue", "Yellow", "Orange", "Magenta", "Green", "Pink", "Lime", "Cyan", "White"}; //to get colorblind text, just use the first letter
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
    int stage = -1;
    List<int> selfAgree = new List<int>();
    List<int> submission = new List<int>();

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { ScreenPress(); return false; };
    }
    void Start()
    {
        float scalar = transform.lossyScale.x;
        for (var i = 0; i < lights.Length; i++)
            lights[i].range *= scalar;

        edgework[0] = Bomb.GetOnIndicators().Count() == Bomb.GetOffIndicators().Count();    //boss
        edgework[1] = Bomb.GetPortPlates().Any(x => x.Length == 0);                         //cruel
        edgework[2] = Bomb.IsPortPresent(Port.Parallel) || Bomb.IsPortPresent(Port.Serial); //faulty
        edgework[3] = Bomb.GetBatteryCount(Battery.D) == 0;                                 //lookalike
        edgework[4] = Bomb.GetSerialNumberNumbers().Last() % 2 == 0;                        //puzzle
        edgework[5] = Bomb.GetBatteryCount() > 1;                                           //simon
        edgework[6] = Bomb.GetBatteryCount() % 2 == 1;                                      //timebased
        edgework[7] = Bomb.GetSerialNumber().Any(ch => "AEIOU".Contains(ch));               //translated

        StartCoroutine(StartFlashes());

        Debug.LogFormat("<Simon Supports #{0}> Edgework conditions: {1} {2} {3} {4} {5} {6} {7} {8}", moduleId, edgework[0], edgework[1], edgework[2], edgework[3], edgework[4], edgework[5], edgework[6], edgework[7]);

        GeneratePuzzle();
        for (int i = 0; i < 5; i++)
        {
            leds[i].GetComponent<MeshRenderer>().material = ledMats[col[i]];
            lights[i].enabled = false;
            if (CB.ColorblindModeActive)
            {
                cbTexts[i].text = colorNames[col[i]][0].ToString();
            }
        }

        Debug.LogFormat("[Simon Supports #{0}] Hello, welcome to Steel Crate Games! Your fellow employees Simon, Simon, Simon, Simon, and Simon sit in front of you, wearing their favorite ties of {1}, {2}, {3}, {4}, {5}.", moduleId, colorNames[col[0]], colorNames[col[1]], colorNames[col[2]], colorNames[col[3]], colorNames[col[4]]);
        Debug.LogFormat("[Simon Supports #{0}] The topics on the agenda today are {1}, {2}, and {3}, but I'm sure you already knew that.", moduleId, traitNames[tra[0]], traitNames[tra[1]], traitNames[tra[2]]);
        if (attempts != 1)
        {
            Debug.LogFormat("[Simon Supports #{0}] Fun Fact!: Due to administrative issues, the scheduled date for the meeting had to be moved a total of {1} time(s)!", moduleId, attempts - 1);
        }
        for (int i = 0; i < 5; i++)
        {
            List<int> temp = new List<int>();
            for (int j = 0; j < 3; j++)
            {
                if (combo[j][i])
                {
                    temp.Add(j + 1);
                }
            }
            switch (temp.Count())
            {
                case 0: Debug.LogFormat("[Simon Supports #{0}] The Simon wearing {1} agrees with none of the topics shown.", moduleId, colorNames[col[i]]); break;
                case 1: Debug.LogFormat("[Simon Supports #{0}] The Simon wearing {1} agrees only with topic {2}.", moduleId, colorNames[col[i]], temp[0]); break;
                case 2: Debug.LogFormat("[Simon Supports #{0}] The Simon wearing {1} agrees with topics {2} and {3}.", moduleId, colorNames[col[i]], temp[0], temp[1]); break;
                case 3: Debug.LogFormat("[Simon Supports #{0}] The Simon wearing {1} agrees with topics {2}, {3}, and {4}.", moduleId, colorNames[col[i]], temp[0], temp[1], temp[2]); break;
            }
        }
        for (int i = 0; i < 3; i++)
        {
            if (edgework[tra[i]])
            {
                selfAgree.Add(i);
            }
        }
        switch (selfAgree.Count())
        {
            case 0: Debug.LogFormat("[Simon Supports #{0}] You, a bold and independent individual, agree with none of the topics shown.", moduleId); break;
            case 1: Debug.LogFormat("[Simon Supports #{0}] You yourself only agree with topic {1}.", moduleId, selfAgree[0] + 1); break;
            case 2: Debug.LogFormat("[Simon Supports #{0}] You yourself agree with topics {1} and {2}.", moduleId, selfAgree[0] + 1, selfAgree[1] + 1); break;
            case 3: Debug.LogFormat("[Simon Supports #{0}] You yourself agree with topics {1}, {2}, and {3}.", moduleId, selfAgree[0] + 1, selfAgree[1] + 1, selfAgree[2] + 1); break;
        }
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


    void ScreenPress()
    {
        if (moduleSolved)
        {
            return;
        }

        if (new int[] { 0, 1, 2 }.Contains(stage))
        {
            Audio.PlaySoundAtTransform(sounds[stage].name, button.transform);
        }
        submission.Add(stage);
        button.AddInteractionPunch(1);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Audio.PlaySoundAtTransform(sounds[stage + 1].name, button.transform);
        if (stage == -1)
        {
            CheckSubmission();
        }
    }

    void CheckSubmission()
    {
        if (submission.Count() == 0)
        {
            return;
        }
        if ((submission.Count() == 1) && (submission[0] == -1))
        {
            Debug.LogFormat("[Simon Supports #{0}] You showed your support for none of the topics.", moduleId, submission[0]);
            if (selfAgree.Count() == 0)
            {
                StartCoroutine(Solve());
            }
            else StartCoroutine(Strike());
        }
        else if (submission.Count() == selfAgree.Count())
        {
            switch (submission.Count())
            {
                case 1:
                    Debug.LogFormat("[Simon Supports #{0}] You showed your support for only topic {1}.", moduleId, submission[0] + 1);
                    if (submission[0] == selfAgree[0])
                    {
                        StartCoroutine(Solve()); break;
                    }
                    else StartCoroutine(Strike()); break;
                case 2:
                    Debug.LogFormat("[Simon Supports #{0}] You showed your support for topics {1} and {2}.", moduleId, submission[0] + 1, submission[1] + 1);
                    if ((submission[0] == selfAgree[0]) && (submission[1] == selfAgree[1]))
                    {
                        StartCoroutine(Solve()); break;
                    }
                    else StartCoroutine(Strike()); break;
                case 3:
                    Debug.LogFormat("[Simon Supports #{0}] You showed your support for all three of the topics.", moduleId);
                    if ((submission[0] == selfAgree[0]) && (submission[1] == selfAgree[1]) && (submission[2] == selfAgree[2]))
                    {
                        StartCoroutine(Solve()); break;
                    }
                    else StartCoroutine(Strike()); break;
                default: StartCoroutine(Strike()); break;
            }
        }
        else StartCoroutine(Strike());
    }


    IEnumerator Solve()
    {
        moduleSolved = true;
        yield return new WaitForSecondsRealtime(0.5f);
        Debug.LogFormat("[Simon Supports #{0}] Which is exactly what the boss wanted! I reckon you're in for a raise. While you're at it, module solved!", moduleId);
        GetComponent<KMBombModule>().HandlePass();
        Audio.PlaySoundAtTransform("clap", button.transform);
    }

    IEnumerator Strike()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        Debug.LogFormat("[Simon Supports #{0}] ...which was not what the boss wanted. Strike incurred and pay docked.", moduleId);
        GetComponent<KMBombModule>().HandleStrike();
        submission.Clear();
    }

    IEnumerator StartFlashes()
    {
        while (!moduleSolved)
        {
            if (stage == -1)
            {
                foreach (Light light in lights)
                {
                    light.enabled = false;
                }
                yield return new WaitForSecondsRealtime(2f);
            }
            stage++;
            for (int i = 0; i < 5; i++)
            {
                if (combo[stage][i] == true)
                {
                    lights[i].enabled = true;
                }   
            }
            yield return new WaitForSeconds(1f);
            foreach (Light light in lights)
            {
                light.enabled = false;
            }
            if (stage == 2)
            {
                stage = -1;
                CheckSubmission();
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} submit 2 3 to support the 2nd and 3rd topics. Use !{0} submit none to support none of the topics.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command)
    {
        string[] parameters = Command.Trim().ToUpperInvariant().Split(' ');
        List<string> submitting = new List<string>();
        for (int i = 1; i < parameters.Length; i++)
        {
            submitting.Add(parameters[i]);
        }
        Debug.Log(submitting.Join());
        if (parameters[0] != "SUBMIT")
        {
            Debug.Log("what");
            yield return "sendtochaterror";
        }
        else if (parameters[1] == "NONE")
        {
            yield return null;
            while (stage != -1)
            {
                yield return "trycancel";
            }
            button.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        else if (submitting.All(x => new string[] {"1","2","3"}.Contains(x)))
        {
            for (int i = 0; i < submitting.Count(); i++)
            {
                yield return null;
                while (stage != int.Parse(submitting[i]) - 1)
                {
                    yield return "trycancel";
                }
                button.OnInteract();
            }
        }

    }
    IEnumerator TwitchHandleForcedSolve ()
    {
        if (selfAgree.Count() == 0)
        {
            while (stage != -1)
            {
                yield return true;
            }
            button.OnInteract();
        }
        foreach (int term in selfAgree)
        {
            while (stage != term)
            {
                yield return null;
            }
            button.OnInteract();
        }
    }
}
