using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class GettinFunkyScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public AudioSource customplayer;
    public AudioClip[] customsounds;
    public AudioClip solve;
    public AudioClip strike;
    public KMSelectable[] buttons;
    public Light[] lights;

    private Coroutine sequence;
    private Coroutine flash;
    private Coroutine strikeplayer;
    private List<int> chosenMoves = new List<int>();
    private int[] table1 = new int[] 
    {
        79, 33, 15, 36, 95, 78,
        68, 67, 53, 44, 8, 90,
        99, 20, 69, 29, 84, 54,
        91, 0, 62, 94, 98, 61,
        87, 65, 3, 32, 81, 55,
        46, 80, 7, 27, 83, 10
    };
    private int[] table2 = new int[]
    {
        43, 48, 17, 11, 93, 45,
        22, 76, 92, 38, 75, 74,
        73, 57, 77, 71, 64, 82,
        58, 25, 6, 52, 86, 42,
        72, 47, 16, 49, 13, 96,
        60, 21, 59, 56, 4, 26
    };
    private string[] moves = new string[] { "\"To the left\"", "\"Left foot now y'all\"", "\"Slide to the left\"", "\"Take it back now, y'all\"", "\"Hands on you knees, hands on your knees\"", "\"To the right now\"", "\"Right foot now\"", "\"Slide to the right\"", "\"One hop this time\"", "\"One hop\"", "\"Two hops this time\"", "\"Two hops, Two hops\"", "\"Five hops this time\"", "\"Right foot, let's stomp\"", "\"Right foot two stomps\"", "\"Left foot, let's stomp\"", "\"Left foot two stomps\"", "\"Turn it out\"", "\"Cha cha now y'all\"", "\"Cha cha real smooth\"", "\"Reverse\"", "\"Reverse, reverse\"", "\"Freeze\"", "\"Criss cross\"", "\"Everybody clap your hands\"", "\"Clap, clap, clap, clap your hands\"", "\"Charlie brown\"" };
    private string input = "";
    private string correct = "";
    private string lastMove = "";

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start () {
        float scalar = transform.lossyScale.x;
        foreach (Light l in lights)
            l.range *= scalar;
        int count = UnityEngine.Random.Range(4, 8);
        for (int i = 0; i < count; i++)
        {
            chosenMoves.Add(UnityEngine.Random.Range(1, customsounds.Length));
        }
        string log = "";
        for (int i = 0; i < count; i++)
        {
            if (i != count - 1)
                log += moves[chosenMoves[i]-1] + ", ";
            else
                log += moves[chosenMoves[i]-1];
        }
        Debug.LogFormat("[Gettin' Funky #{0}] The set of moves is as follows: {1}", moduleId, log);
        correct = CalculateAnswer();
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            if (buttons[0] == pressed)
            {
                if (sequence != null)
                {
                    StopCoroutine(sequence);
                    sequence = null;
                    customplayer.Stop();
                }
                sequence = StartCoroutine(PlaySequence());
            }
            else if (buttons[1] == pressed)
            {
                if (sequence != null)
                {
                    StopCoroutine(sequence);
                    sequence = null;
                    customplayer.Stop();
                }
                if (strikeplayer != null)
                {
                    StopCoroutine(strikeplayer);
                    strikeplayer = null;
                }
                customplayer.clip = customsounds[0];
                customplayer.Play();
                if (flash != null)
                {
                    StopCoroutine(flash);
                    flash = null;
                    lights[0].enabled = false;
                    lights[1].enabled = false;
                }
                flash = StartCoroutine(FlashButton(0));
                input += "1";
                if (!input.Equals(correct.Substring(0, input.Length)))
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    input = "";
                    strikeplayer = StartCoroutine(StrikeSound());
                }
                else if (input.Length == correct.Length)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    StartCoroutine(SolveSound());
                }
            }
            else if (buttons[2] == pressed)
            {
                if (sequence != null)
                {
                    StopCoroutine(sequence);
                    sequence = null;
                    customplayer.Stop();
                }
                if (strikeplayer != null)
                {
                    StopCoroutine(strikeplayer);
                    strikeplayer = null;
                }
                customplayer.clip = customsounds[0];
                customplayer.Play();
                if (flash != null)
                {
                    StopCoroutine(flash);
                    flash = null;
                    lights[0].enabled = false;
                    lights[1].enabled = false;
                }
                flash = StartCoroutine(FlashButton(1));
                input += "0";
                if (!input.Equals(correct.Substring(0, input.Length)))
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    input = "";
                    strikeplayer = StartCoroutine(StrikeSound());
                }
                else if (input.Length == correct.Length)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    StartCoroutine(SolveSound());
                }
            }
        }
    }

    private string CalculateAnswer()
    {
        List<int> allTable = new List<int>();
        List<int> allPos = new List<int>();
        int[][] tables = new int[][] { table1, table2 };
        string correct = "";
        string start = "" + bomb.GetSerialNumberNumbers().ElementAt(0) + bomb.GetSerialNumberNumbers().ElementAt(1);
        int curTable = 0;
        int curPos = 0;
        bool ignore = false;
        bool ignorelog = false;
        if (table1.Contains(int.Parse(start)))
        {
            curPos = Array.IndexOf(table1, int.Parse(start));
            Debug.LogFormat("[Gettin' Funky #{0}] The first two digits of the serial number form a number that is in one of the tables", moduleId);
            Debug.LogFormat("[Gettin' Funky #{0}] Starting in the left table at the cell with the number {1}", moduleId, start);
        }
        else if (table2.Contains(int.Parse(start)))
        {
            curTable = 1;
            curPos = Array.IndexOf(table2, int.Parse(start));
            Debug.LogFormat("[Gettin' Funky #{0}] The first two digits of the serial number form a number that is in one of the tables", moduleId);
            Debug.LogFormat("[Gettin' Funky #{0}] Starting in the right table at the cell with the number {1}", moduleId, start);
        }
        else
        {
            for (int i = 0; i < bomb.GetSerialNumberNumbers().Count(); i++)
            {
                curPos += bomb.GetSerialNumberNumbers().ElementAt(i);
            }
            if (curPos == 36)
                curPos = 0;
            if (!bomb.IsPortPresent(Port.RJ45))
                curTable = 1;
            Debug.LogFormat("[Gettin' Funky #{0}] The first two digits of the serial number form a number that is not in one of the tables", moduleId);
            Debug.LogFormat("[Gettin' Funky #{0}] Starting in the {1} table at the cell with the number {2}", moduleId, curTable == 0 ? "left" : "right", tables[curTable][curPos] < 10 ? "0" + tables[curTable][curPos] : "" + tables[curTable][curPos]);
        }
        Debug.LogFormat("[Gettin' Funky #{0}] =======Table Moves=======", moduleId);
        for (int i = 0; i < chosenMoves.Count; i++)
        {
            string move = "";
            string before = tables[curTable][curPos] < 10 ? "0" + tables[curTable][curPos] : "" + tables[curTable][curPos];
            allTable.Add(curTable);
            allPos.Add(curPos);
            switch (chosenMoves[i])
            {
                case 1:
                case 2:
                    if (!ignore)
                        lastMove = "L";
                    if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                        curPos += 5;
                    else
                        curPos--;
                    move = "Left one cell";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 3:
                    if (!ignore)
                        lastMove = "L";
                    for (int j = 0; j < 3; j++)
                    {
                        if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                            curPos += 5;
                        else
                            curPos--;
                    }
                    move = "Left three cells";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 4:
                    if (!ignore)
                        lastMove = "D";
                    if (curPos == 30 || curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34 || curPos == 35)
                        curPos -= 30;
                    else
                        curPos += 6;
                    move = "Down one cell";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 5:
                    if (!ignore)
                        lastMove = "D";
                    for (int j = 0; j < 2; j++)
                    {
                        if (curPos == 30 || curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34 || curPos == 35)
                            curPos -= 30;
                        else
                            curPos += 6;
                    }
                    move = "Down two cells";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 6:
                case 7:
                    if (!ignore)
                        lastMove = "R";
                    if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                        curPos -= 5;
                    else
                        curPos++;
                    move = "Right one cell";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 8:
                    if (!ignore)
                        lastMove = "R";
                    for (int j = 0; j < 3; j++)
                    {
                        if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                            curPos -= 5;
                        else
                            curPos++;
                    }
                    move = "Right three cells";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 9:
                case 10:
                    if (!ignore)
                        lastMove = "U";
                    if (curPos == 0 || curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4 || curPos == 5)
                        curPos += 30;
                    else
                        curPos -= 6;
                    move = "Up one cell";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 11:
                case 12:
                    if (!ignore)
                        lastMove = "U";
                    for (int j = 0; j < 2; j++)
                    {
                        if (curPos == 0 || curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4 || curPos == 5)
                            curPos += 30;
                        else
                            curPos -= 6;
                    }
                    move = "Up two cells";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 13:
                    if (!ignore)
                        lastMove = "U";
                    for (int j = 0; j < 5; j++)
                    {
                        if (curPos == 0 || curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4 || curPos == 5)
                            curPos += 30;
                        else
                            curPos -= 6;
                    }
                    move = "Up five cells";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 14:
                    if (!ignore)
                        lastMove = "R";
                    curTable = curTable == 0 ? 1 : 0;
                    if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                        curPos -= 5;
                    else
                        curPos++;
                    move = "Switch table and right one cell";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 15:
                    if (!ignore)
                        lastMove = "R";
                    curTable = curTable == 0 ? 1 : 0;
                    for (int j = 0; j < 2; j++)
                    {
                        if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                            curPos -= 5;
                        else
                            curPos++;
                    }
                    move = "Switch table and right two cells";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 16:
                    if (!ignore)
                        lastMove = "L";
                    curTable = curTable == 0 ? 1 : 0;
                    if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                        curPos += 5;
                    else
                        curPos--;
                    move = "Switch table and left one cell";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 17:
                    if (!ignore)
                        lastMove = "L";
                    curTable = curTable == 0 ? 1 : 0;
                    for (int j = 0; j < 2; j++)
                    {
                        if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                            curPos += 5;
                        else
                            curPos--;
                    }
                    move = "Switch table and left two cells";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 18:
                    string store = lastMove;
                    if (lastMove == "")
                        lastMove = "R";
                    if (lastMove == "R")
                    {
                        lastMove = "D";
                        if (curPos == 30 || curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34 || curPos == 35)
                            curPos -= 30;
                        else
                            curPos += 6;
                    }
                    else if (lastMove == "D")
                    {
                        lastMove = "L";
                        if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                            curPos += 5;
                        else
                            curPos--;
                    }
                    else if (lastMove == "L")
                    {
                        lastMove = "U";
                        if (curPos == 0 || curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4 || curPos == 5)
                            curPos += 30;
                        else
                            curPos -= 6;
                    }
                    else if (lastMove == "U")
                    {
                        lastMove = "R";
                        if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                            curPos -= 5;
                        else
                            curPos++;
                    }
                    else if (lastMove == "UL")
                    {
                        lastMove = "UR";
                        if (curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                            curPos -= 11;
                        else if (curPos == 5)
                            curPos = 30;
                        else if (curPos == 0 || curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4)
                            curPos += 31;
                        else
                            curPos -= 5;
                    }
                    else if (lastMove == "UR")
                    {
                        lastMove = "DR";
                        if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29)
                            curPos++;
                        else if (curPos == 35)
                            curPos = 0;
                        else if (curPos == 30 || curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34)
                            curPos -= 29;
                        else
                            curPos += 7;
                    }
                    else if (lastMove == "DR")
                    {
                        lastMove = "DL";
                        if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24)
                            curPos += 11;
                        else if (curPos == 30)
                            curPos = 5;
                        else if (curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34 || curPos == 35)
                            curPos -= 31;
                        else
                            curPos += 5;
                    }
                    else if (lastMove == "DL")
                    {
                        lastMove = "UL";
                        if (curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4 || curPos == 5)
                            curPos += 29;
                        else if (curPos == 0)
                            curPos = 35;
                        else if (curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                            curPos--;
                        else
                            curPos -= 7;
                    }
                    move = "90 degrees clockwise and one cell in that direction";
                    if (ignore)
                    {
                        lastMove = store;
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 19:
                case 20:
                    store = lastMove;
                    if (lastMove == "")
                        lastMove = "L";
                    if (lastMove == "L")
                    {
                        lastMove = "D";
                        if (curPos == 30 || curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34 || curPos == 35)
                            curPos -= 30;
                        else
                            curPos += 6;
                    }
                    else if (lastMove == "U")
                    {
                        lastMove = "L";
                        if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                            curPos += 5;
                        else
                            curPos--;
                    }
                    else if (lastMove == "R")
                    {
                        lastMove = "U";
                        if (curPos == 0 || curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4 || curPos == 5)
                            curPos += 30;
                        else
                            curPos -= 6;
                    }
                    else if (lastMove == "D")
                    {
                        lastMove = "R";
                        if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                            curPos -= 5;
                        else
                            curPos++;
                    }
                    else if (lastMove == "DR")
                    {
                        lastMove = "UR";
                        if (curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29 || curPos == 35)
                            curPos -= 11;
                        else if (curPos == 5)
                            curPos = 30;
                        else if (curPos == 0 || curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4)
                            curPos += 31;
                        else
                            curPos -= 5;
                    }
                    else if (lastMove == "DL")
                    {
                        lastMove = "DR";
                        if (curPos == 5 || curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29)
                            curPos++;
                        else if (curPos == 35)
                            curPos = 0;
                        else if (curPos == 30 || curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34)
                            curPos -= 29;
                        else
                            curPos += 7;
                    }
                    else if (lastMove == "UL")
                    {
                        lastMove = "DL";
                        if (curPos == 0 || curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24)
                            curPos += 11;
                        else if (curPos == 30)
                            curPos = 5;
                        else if (curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34 || curPos == 35)
                            curPos -= 31;
                        else
                            curPos += 5;
                    }
                    else if (lastMove == "UR")
                    {
                        lastMove = "UL";
                        if (curPos == 1 || curPos == 2 || curPos == 3 || curPos == 4 || curPos == 5)
                            curPos += 29;
                        else if (curPos == 0)
                            curPos = 35;
                        else if (curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24 || curPos == 30)
                            curPos--;
                        else
                            curPos -= 7;
                    }
                    move = "90 degrees counter-clockwise and one cell in that direction";
                    if (ignore)
                    {
                        lastMove = store;
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 21:
                case 22:
                    if (allTable.Count != 1)
                    {
                        curTable = allTable[i - 1];
                        curPos = allPos[i - 1];
                        move = "Undo previous";
                    }
                    else
                    {
                        move = "Undo previous (first move so no effect)";
                    }
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 23:
                    if (i == chosenMoves.Count - 1)
                    {
                        move = "Ignore next (last move so no effect)";
                        if (ignore)
                        {
                            move += " [Ignored]";
                            ignorelog = true;
                        }
                    }
                    else
                    {
                        if (!ignore)
                        {
                            ignore = true;
                            move = "Ignore next";
                        }
                        else
                        {
                            move = "Ignore next [Ignored]";
                            ignorelog = true;
                        }
                    }
                    break;
                case 24:
                    store = lastMove;
                    string[] pos = new string[] { "UL", "UR", "DL", "DR" };
                    int[] offsets = new int[] { -7, -5, 5, 7 };
                    if (curPos == 0)
                    {
                        curPos = 7;
                        lastMove = pos[3];
                    }
                    else if (curPos == 5)
                    {
                        curPos = 10;
                        lastMove = pos[2];
                    }
                    else if (curPos == 30)
                    {
                        curPos = 25;
                        lastMove = pos[1];
                    }
                    else if (curPos == 35)
                    {
                        curPos = 28;
                        lastMove = pos[0];
                    }
                    else if (curPos > 0 && curPos < 5)
                    {
                        int[] compare = new int[4];
                        compare[0] = -1;
                        compare[1] = -1;
                        compare[2] = tables[curTable][curPos + 5];
                        compare[3] = tables[curTable][curPos + 7];
                        int temp = HighPos(compare);
                        curPos += offsets[temp];
                        lastMove = pos[temp];
                    }
                    else if (curPos > 30 && curPos < 35)
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 7];
                        compare[1] = tables[curTable][curPos - 5];
                        compare[2] = -1;
                        compare[3] = -1;
                        int temp = HighPos(compare);
                        curPos += offsets[temp];
                        lastMove = pos[temp];
                    }
                    else if (curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24)
                    {
                        int[] compare = new int[4];
                        compare[0] = -1;
                        compare[1] = tables[curTable][curPos - 5];
                        compare[2] = -1;
                        compare[3] = tables[curTable][curPos + 7];
                        int temp = HighPos(compare);
                        curPos += offsets[temp];
                        lastMove = pos[temp];
                    }
                    else if (curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29)
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 7];
                        compare[1] = -1;
                        compare[2] = tables[curTable][curPos + 5];
                        compare[3] = -1;
                        int temp = HighPos(compare);
                        curPos += offsets[temp];
                        lastMove = pos[temp];
                    }
                    else
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 7];
                        compare[1] = tables[curTable][curPos - 5];
                        compare[2] = tables[curTable][curPos + 5];
                        compare[3] = tables[curTable][curPos + 7];
                        int temp = HighPos(compare);
                        curPos += offsets[temp];
                        lastMove = pos[temp];
                    }
                    move = "Diagonally adjacent cell with highest number";
                    if (ignore)
                    {
                        lastMove = store;
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 25:
                case 26:
                    store = lastMove;
                    string[] pos2 = new string[] { "U", "R", "D", "L" };
                    int[] offsets2 = new int[] { -6, 1, 6, -1 };
                    if (curPos == 0)
                    {
                        int[] compare = new int[4];
                        compare[0] = 100;
                        compare[1] = tables[curTable][curPos + 1];
                        compare[2] = tables[curTable][curPos + 6];
                        compare[3] = 100;
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else if (curPos == 5)
                    {
                        int[] compare = new int[4];
                        compare[0] = 100;
                        compare[1] = 100;
                        compare[2] = tables[curTable][curPos + 6];
                        compare[3] = tables[curTable][curPos - 1];
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else if (curPos == 30)
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 6];
                        compare[1] = tables[curTable][curPos + 1];
                        compare[2] = 100;
                        compare[3] = 100;
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else if (curPos == 35)
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 6];
                        compare[1] = 100;
                        compare[2] = 100;
                        compare[3] = tables[curTable][curPos - 1];
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else if (curPos > 0 && curPos < 5)
                    {
                        int[] compare = new int[4];
                        compare[0] = 100;
                        compare[1] = tables[curTable][curPos + 1];
                        compare[2] = tables[curTable][curPos + 6];
                        compare[3] = tables[curTable][curPos - 1];
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else if (curPos > 30 && curPos < 35)
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 6];
                        compare[1] = tables[curTable][curPos + 1];
                        compare[2] = 100;
                        compare[3] = tables[curTable][curPos - 1];
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else if (curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24)
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 6];
                        compare[1] = tables[curTable][curPos + 1];
                        compare[2] = tables[curTable][curPos + 6];
                        compare[3] = 100;
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else if (curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29)
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 6];
                        compare[1] = 100;
                        compare[2] = tables[curTable][curPos + 6];
                        compare[3] = tables[curTable][curPos - 1];
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    else
                    {
                        int[] compare = new int[4];
                        compare[0] = tables[curTable][curPos - 6];
                        compare[1] = tables[curTable][curPos + 1];
                        compare[2] = tables[curTable][curPos + 6];
                        compare[3] = tables[curTable][curPos - 1];
                        int temp = LowPos(compare);
                        curPos += offsets2[temp];
                        lastMove = pos2[temp];
                    }
                    move = "Orthogonally adjacent cell with lowest number";
                    if (ignore)
                    {
                        lastMove = store;
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                case 27:
                    int index = i + 1;
                    for (int j = 0; j < index; j++)
                    {
                        if (curPos == 30 || curPos == 31 || curPos == 32 || curPos == 33 || curPos == 34 || curPos == 35)
                        {
                            curTable = curTable == 0 ? 1 : 0;
                            curPos -= 30;
                        }
                        else
                            curPos += 6;
                    }
                    move = "Go down " + index + " cells and switch table on loop";
                    if (ignore)
                    {
                        move += " [Ignored]";
                        ignorelog = true;
                    }
                    break;
                default:
                    break;
            }
            string after = tables[curTable][curPos] < 10 ? "0" + tables[curTable][curPos] : "" + tables[curTable][curPos];
            if (ignore && !ignorelog)
            {
                Debug.LogFormat("[Gettin' Funky #{0}] {1} | {2}", moduleId, after, move);
            }
            else
            {
                Debug.LogFormat("[Gettin' Funky #{0}] {1} -> {2} | {3}", moduleId, before, after, move);
            }
            if (ignorelog)
            {
                curTable = allTable[i];
                curPos = allPos[i];
                ignorelog = false;
                ignore = false;
            }
        }
        Debug.LogFormat("[Gettin' Funky #{0}] ======================", moduleId);
        correct = Convert.ToString(tables[curTable][curPos], 2);
        while (correct.Length != 7)
            correct = correct.Insert(0, "0");
        Debug.LogFormat("[Gettin' Funky #{0}] {1} converted to binary is {2}", moduleId, tables[curTable][curPos], correct);
        return correct;
    }

    private int HighPos(int[] comp)
    {
        int high = -1;
        int val = -1;
        for (int i = 0; i < comp.Length; i++)
        {
            if (comp[i] > val)
            {
                val = comp[i];
                high = i;
            }
        }
        return high;
    }
    private int LowPos(int[] comp)
    {
        int low = -1;
        int val = 100;
        for (int i = 0; i < comp.Length; i++)
        {
            if (comp[i] < val)
            {
                val = comp[i];
                low = i;
            }
        }
        return low;
    }


    private IEnumerator PlaySequence()
    {
        for (int i = 0; i < chosenMoves.Count; i++)
        {
            customplayer.clip = customsounds[chosenMoves[i]];
            customplayer.Play();
            while (customplayer.isPlaying) { yield return null; }
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator FlashButton(int n)
    {
        lights[n].enabled = true;
        yield return new WaitForSeconds(.49f);
        lights[n].enabled = false;
    }

    private IEnumerator SolveSound()
    {
        yield return new WaitForSeconds(.65f);
        customplayer.clip = solve;
        customplayer.Play();
    }

    private IEnumerator StrikeSound()
    {
        yield return new WaitForSeconds(.65f);
        customplayer.clip = strike;
        customplayer.Play();
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} play [Presses the red button] | !{0} press <l/r> [Presses the left or right arrow button] | Presses are chainable, for ex: !{0} press lrrlr";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*play\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[0].OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                for (int i = 0; i < parameters[1].Length; i++)
                {
                    if (!(parameters[1][i] == 'l' || parameters[1][i] == 'L' || parameters[1][i] == 'r' || parameters[1][i] == 'R'))
                    {
                        yield return "sendtochaterror The specified arrow button '" + parameters[1][i] + "' is invalid!";
                        yield break;
                    }
                }
                for (int i = 0; i < parameters[1].Length; i++)
                {
                    if (parameters[1][i] == 'l' || parameters[1][i] == 'L')
                        buttons[1].OnInteract();
                    else
                        buttons[2].OnInteract();
                    yield return new WaitForSeconds(.49f);
                }
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the arrow button(s) you wish to press!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        int start = input.Length;
        for (int i = start; i < 7; i++)
        {
            if (correct[i].Equals('1'))
                buttons[1].OnInteract();
            else if (correct[i].Equals('0'))
                buttons[2].OnInteract();
            yield return new WaitForSeconds(.49f);
        }
    }
}
