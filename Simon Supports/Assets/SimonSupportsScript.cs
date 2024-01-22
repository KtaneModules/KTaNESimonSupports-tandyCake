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
    public MeshRenderer[] leds;
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
    private bool lightTurnedGreen;
    [SerializeField]
    bool cbON = false;
    int postSolveCounter = 0;
    List<int>[] flashes = new List<int>[5].Select(x => new List<int>()).ToArray();

    void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { ScreenPress(); return false; };
    }
    void Start()
    {
        if (CB.ColorblindModeActive)
            cbON = true;
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
        CBToggle();
        
        for (int i = 0; i < 5; i++)
            leds[i].material = ledMats[col[i]];

        Debug.LogFormat("[Simon Supports #{0}] Hello, welcome to Steel Crate Games! Your fellow employees Simon, Simon, Simon, Simon, and Simon sit in front of you, wearing their favorite ties of {1}, {2}, {3}, {4}, {5}.", moduleId, colorNames[col[0]], colorNames[col[1]], colorNames[col[2]], colorNames[col[3]], colorNames[col[4]]);
        Debug.LogFormat("[Simon Supports #{0}] The topics on the agenda today are {1}, {2}, and {3}, but I'm sure you already knew that.", moduleId, traitNames[tra[0]], traitNames[tra[1]], traitNames[tra[2]]);
        if (attempts != 1)
            Debug.LogFormat("[Simon Supports #{0}] Fun Fact!: Due to administrative issues, the scheduled date for the meeting had to be moved a total of {1} time{2}!", moduleId, attempts - 1, attempts == 2 ? "" : "s");
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
                selfAgree.Add(i);
        }
        switch (selfAgree.Count())
        {
            case 0: Debug.LogFormat("[Simon Supports #{0}] You, a bold and independent individual, agree with none of the topics shown.", moduleId); break;
            case 1: Debug.LogFormat("[Simon Supports #{0}] You yourself only agree with topic {1}.", moduleId, selfAgree[0] + 1); break;
            case 2: Debug.LogFormat("[Simon Supports #{0}] You yourself agree with topics {1} and {2}.", moduleId, selfAgree[0] + 1, selfAgree[1] + 1); break;
            case 3: Debug.LogFormat("[Simon Supports #{0}] You yourself agree with topics {1}, {2}, and {3}.", moduleId, selfAgree[0] + 1, selfAgree[1] + 1, selfAgree[2] + 1); break;
        }
    }

    void CBToggle()
    {
        for (int i = 0; i < 5; i++)
        {
            if (cbON)
                cbTexts[i].text = colorNames[col[i]][0].ToString();
            else cbTexts[i].text = string.Empty;
        }
    }
    //Blan code
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
        for (int c = 0; c < 5; c++)
            for (int f = 0; f < 3; f++)
                if (combo[f][c])
                    flashes[c].Add(f);
        if (Enumerable.Range(0, 3).Any(flashIx => !flashes.Any(led => led.Contains(flashIx))))
            goto Restart;
    }
    //also blan code
    void Check () {
        int[] x = { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2 };
        int[] y = { 1, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 3, 4, 5, 6, 7 };

        for (int z = 0; z < x.Count(); z++) {
            if (Same(x[z], y[z]))  generationValid = false; ;
        }
    }
    //does blan know sequenceequal is a method
    bool Same (int a, int b) {
        return (combo[a][0] == combo[b][0] && combo[a][1] == combo[b][1] && combo[a][2] == combo[b][2] && combo[a][3] == combo[b][3] && combo[a][4] == combo[b][4]);
    }


    void ScreenPress()
    {
        button.AddInteractionPunch(1);
        if (moduleSolved)
        {
            PressOnSolve();
            return;
        }
        Debug.Log("PRESSED " + stage);

        if (stage != -1)
            Audio.PlaySoundAtTransform(sounds[stage].name, button.transform);
        if (!submission.Contains(stage))
            submission.Add(stage);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Audio.PlaySoundAtTransform(sounds[stage + 1].name, button.transform);
        if (stage == -1)
            CheckSubmission();
    }

    void CheckSubmission()
    {
        if (submission.Count() == 0)
            return;
        if ((submission.Count() == 1) && (submission[0] == -1))
        {
            Debug.LogFormat("[Simon Supports #{0}] You showed your support for none of the topics.", moduleId, submission[0]);
            if (selfAgree.Count() == 0)
                StartCoroutine(Solve());
            else StartCoroutine(Strike());
        }
        else
        {
            switch (submission.Count())
            {
                case 1: Debug.LogFormat("[Simon Supports #{0}] You showed your support for only topic {1}.", moduleId, submission[0] + 1); 
                    break;
                case 2: Debug.LogFormat("[Simon Supports #{0}] You showed your support for topics {1} and {2}.", moduleId, submission[0] + 1, submission[1] + 1); 
                    break;
                case 3: Debug.LogFormat("[Simon Supports #{0}] You showed your support for all three of the topics.", moduleId);
                    break;
                default: break;
            }
            if (submission.Count() == selfAgree.Count() && submission.SequenceEqual(selfAgree))
                StartCoroutine(Solve());
            else StartCoroutine(Strike());
        }
    }

    void PressOnSolve()
    {
        int[] order = new int[] { 0, 1, 2, 3, 2, 1 }.ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray().ToArray();
        Audio.PlaySoundAtTransform(sounds[order[postSolveCounter % 6]].name, transform);
        postSolveCounter++;
    }


    IEnumerator Solve()
    {
        moduleSolved = true;
        yield return new WaitForSecondsRealtime(0.5f);
        Debug.LogFormat("[Simon Supports #{0}] Which is exactly what the boss wanted! I reckon you're in for a raise. While you're at it, module solved!", moduleId);
        GetComponent<KMBombModule>().HandlePass();
        lightTurnedGreen = true;
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
                for (int i = 0; i < 5; i++)
                {
                    leds[i].material = ledMats[col[i]];
                    cbTexts[i].color = Color.black;
                }
                yield return new WaitForSecondsRealtime(2f);
            }
            stage++;
            for (int i = 0; i < 5; i++)
            {
                if (combo[stage][i] == true)
                {
                    leds[i].material = ledMats[col[i] + 10];
                    cbTexts[i].color = Color.white;
                }
            }
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < 5; i++)
            {
                leds[i].material = ledMats[col[i]];
                cbTexts[i].color = Color.black;
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
    private readonly string TwitchHelpMessage = @"Use !{0} submit 2 3 to support the 2nd and 3rd topics. Use !{0} submit none to support none of the topics. Use !{0} colorblind to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator Press(float delay)
    {
        button.OnInteract();
        yield return new WaitForSeconds(delay);
    }
    IEnumerator ProcessTwitchCommand (string input)
    {
        string command = input.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (new string[] { "COLORBLIND", "COLOURBLIND", "CB", "COLOR-BLIND", "COLOUR-BLIND"}.Contains(command))
        {
            yield return null;
            cbON = !cbON;
            CBToggle();
            yield break;
        }
        else if (parameters.First() != "SUBMIT" || parameters.Count <= 1)
            yield return "sendtochaterror Invalid command";
        parameters.Remove("SUBMIT");
        if (parameters.Count == 1 && parameters[0] == "NONE")
        {
            yield return null;
            while (stage != -1)
                yield return "trycancel";
            yield return Press(0.1f);
        }
        else if (parameters.All(x => new string[] { "1", "2", "3" }.Contains(x)))
        {
            yield return null;
            foreach (string digit in parameters)
            {
                int submit = int.Parse(digit) - 1;
                while (stage != submit)
                    yield return null;
                yield return Press(0);
            }
            yield return submission.SequenceEqual(selfAgree) ? "solve" : "strike";
        }
    }
    IEnumerator TwitchHandleForcedSolve ()
    {
        if (selfAgree.Count() == 0)
        {
            while (stage != -1)
                yield return true;
            yield return Press(0.1f);
        }
        else
        {
            while (stage != selfAgree.First())
                yield return true;
            foreach (int term in selfAgree)
            {
                yield return new WaitUntil(() => stage == term);
                yield return Press(0);
            }
        }
        while (!lightTurnedGreen)
            yield return true;
    }
}
