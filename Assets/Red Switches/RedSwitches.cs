using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class RedSwitches : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] Switches;
    public MeshRenderer[] LedsUp;
    public MeshRenderer[] LedsDown;
    public Material[] LedMaterials;

    private const int STARTING_MOVES = 11;
    private const float SWITCH_ANGLE = 55.0f;

    private bool[] switchPositions = new bool[5];
    private bool[] goalPositions = new bool[5];
    private int[] switchToggles = new int[5];
    private int[] goalToggles = new int[5];

    private bool canFlip = false;
    private int lastSwitch = -1;

    private int[] initialMovements = new int[STARTING_MOVES];
    private int[][] switchOptions = new int[5][];

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;


    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        for (int i = 0; i < Switches.Length; i++) {
            int j = i;
            Switches[i].OnInteract += delegate () { PressSwitch(j); return false; };
        }
    }

    // Initiates the module
    private void Start() {
        // Determines the initial state and goals of the switches
        for (int i = 0; i < switchPositions.Length; i++) {
            switchPositions[i] = UnityEngine.Random.Range(0, 2) == 1 ? true : false;
            goalPositions[i] = switchPositions[i];
        }

        // Determines which switch toggles which LEDs
        switchToggles = RandomizeToggles(switchToggles);
        goalToggles = RandomizeToggles(goalToggles);

        // Makes initial movements on the module from the starting position
        switchOptions[0] = new int[4] { 1, 2, 3, 4 };
        switchOptions[1] = new int[4] { 0, 2, 3, 4 };
        switchOptions[2] = new int[4] { 0, 1, 3, 4 };
        switchOptions[3] = new int[4] { 0, 1, 2, 4 };
        switchOptions[4] = new int[4] { 0, 1, 2, 3 };

        int lastMove = -1, randMove = 0;
        for (int i = 0; i < initialMovements.Length; i++) {
            if (lastMove == -1)
                randMove = UnityEngine.Random.Range(0, 5);

            else
                randMove = switchOptions[lastMove][UnityEngine.Random.Range(0, 4)];

            FlipSwitch(randMove, false);
            lastMove = randMove;
            initialMovements[i] = randMove;
        }

        // Renders the switch and LED positions on the module
        DisplayLeds();

        for (int i = 0; i < Switches.Length; i++) {
            if (switchPositions[i])
                Switches[i].transform.localEulerAngles = new Vector3(SWITCH_ANGLE, 0.0f, 0.0f);

            else
                Switches[i].transform.localEulerAngles = new Vector3(-SWITCH_ANGLE, 0.0f, 0.0f);
        }

        // Logs all the information
        Debug.LogFormat("[Red Switches #{0}] The initial states of the switches are: {1}, {2}, {3}, {4}, {5}", moduleId,
            FormatState(switchPositions[0]), FormatState(switchPositions[1]), FormatState(switchPositions[2]), FormatState(switchPositions[3]), FormatState(switchPositions[4]));
        Debug.LogFormat("[Red Switches #{0}] The initial goal states of the switches are: {1}, {2}, {3}, {4}, {5}", moduleId,
            FormatState(goalPositions[0]), FormatState(goalPositions[1]), FormatState(goalPositions[2]), FormatState(goalPositions[3]), FormatState(goalPositions[4]));

        for (int i = 0; i < switchToggles.Length; i++)
            Debug.LogFormat("[Red Switches #{0}] Switch {1} toggles switch {2} and the goal of switch {3}.", moduleId, FormatNum(i), FormatNum(switchToggles[i]), FormatNum(goalToggles[i]));

        string movesStr = "";
        for (int i = initialMovements.Length - 1; i >= 0; i--) {
            movesStr += FormatNum(initialMovements[i]);

            if (i > 0)
                movesStr += ", ";

            else
                movesStr += ".";
        }

        Debug.LogFormat("[Red Switches #{0}] One suggested solution is: {1}", moduleId, movesStr);

        canFlip = true;
    }


    // Randomizes the goal and switch toggles 
    private int[] RandomizeToggles(int[] toggles) {
        int[] openPoses = new int[5];
        for (int i = 0; i < openPoses.Length; i++)
            openPoses[i] = i;

        for (int i = 0; i < toggles.Length; i++) {
            int rand = -1, selected = -1, attempts = 0;

            do {
                rand = UnityEngine.Random.Range(0, 5 - i);
                selected = openPoses[rand];
                attempts++;
            } while (selected == i && attempts < 10);

            if (attempts >= 10) { // Failsafe
                rand = 4 - i;
                selected = openPoses[rand];
            }

            if (selected == i) { // Bugfix
                toggles[i] = toggles[i - 1];
                toggles[i - 1] = selected;
            }

            else
                toggles[i] = selected;

            for (int j = rand; j < openPoses.Length - 1; j++)
                openPoses[j] = openPoses[j + 1];
        }

        return toggles;
    }

    // Changes the switch position
    private void FlipSwitch(int i, bool update) {
        switchPositions[i] = switchPositions[i] ? false : true;
        switchPositions[switchToggles[i]] = switchPositions[switchToggles[i]] ? false : true;
        goalPositions[goalToggles[i]] = goalPositions[goalToggles[i]] ? false : true;

        if (update) {
            DisplayLeds();

            // Check solve condition
            bool correct = true;
            for (int j = 0; j < switchPositions.Length; j++) {
                if (switchPositions[j] != goalPositions[j]) {
                    correct = false;
                    break;
                }
            }

            if (correct)
                Solve();
        }
    }

    // Converts booleans to up/down for logging
    private string FormatState(bool state) {
        return state ? "up" : "down";
    }

    // Converts positions for logging
    private string FormatNum(int num) {
        return (num + 1).ToString();
    }

    // Displays all the LEDs
    private void DisplayLeds() {
        for (int i = 0; i < goalPositions.Length; i++) {
            if (goalPositions[i]) {
                LedsUp[i].material = LedMaterials[1];
                LedsDown[i].material = LedMaterials[0];
            }

            else {
                LedsUp[i].material = LedMaterials[0];
                LedsDown[i].material = LedMaterials[1];
            }
        }
    }


    // Pressing on a switch
    private void PressSwitch(int i) {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Switches[i].transform);
        Switches[i].AddInteractionPunch(.25f);

        if (canFlip) {
            canFlip = false;

            if (i == lastSwitch)
                Strike();

            lastSwitch = i;

            FlipSwitch(i, true);
            StartCoroutine(AnimateSwitch(i));
            StartCoroutine(AnimateSwitch(switchToggles[i]));
        }
    }

    // Animates the switch
    private IEnumerator AnimateSwitch(int i) {
        /// Code from Colored Switches - Written by Timwi
        var switchFrom = switchPositions[i] ? -SWITCH_ANGLE : SWITCH_ANGLE;
        var switchTo = switchPositions[i] ? SWITCH_ANGLE : -SWITCH_ANGLE;

        var startTime = Time.fixedTime;
        const float duration = 0.3f;

        do {
            Switches[i].transform.localEulerAngles = new Vector3(Easing.OutSine(Time.fixedTime - startTime, switchFrom, switchTo, duration), 0, 0);
            yield return null;
        } while (Time.fixedTime < startTime + duration);

        Switches[i].transform.localEulerAngles = new Vector3(switchTo, 0, 0);
        canFlip = true;
    }


    // Module solves
    private void Solve() {
        if (!moduleSolved) {
            moduleSolved = true;
            Debug.LogFormat("[Red Switches #{0}] You flipped the switches to the right positions! Module solved!", moduleId);
            GetComponent<KMBombModule>().HandlePass();
        }
    }

    // Module strikes
    private void Strike() {
        Debug.LogFormat("[Red Switches #{0}] You flipped the same switch twice in a row! Strike!", moduleId);
        GetComponent<KMBombModule>().HandleStrike();
        positionsToggled.Clear();
    }

    // Twitch Plays
#pragma warning disable IDE0051 // Remove unused private members
    readonly string TwitchHelpMessage = "\"!{0} flip 1 2 3 4 5\" [Flips the 1st, 2nd, 3rd, 4th, and 5th switches from left to right. \"flip\" is optional.]";
#pragma warning restore IDE0051 // Remove unused private members

    IEnumerator ProcessTwitchCommand(string cmd)
    {
        var intCmd = cmd.Trim().ToLowerInvariant();
        if (intCmd.StartsWith("flip"))
            intCmd = intCmd.Substring(4).Trim();
        List<int> switchPositions = new List<int>();
        foreach (var posPosition in intCmd.Split())
        {
            int pos;
            if (!int.TryParse(posPosition, out pos) || pos < 1 || pos > 5)
            {
                yield return string.Format("sendtochaterror \"{0}\" does not correspond to a valid position on the module!", posPosition);
                yield break;
            }
            switchPositions.Add(pos - 1);
        }
        if (!switchPositions.Any())
        {
            yield return "sendtochaterror No switches have been specified! Specify at least 1 number in order to flip these.";
            yield break;
        }
        yield return null;
        for (var x = 0; x < switchPositions.Count; x++)
        {
            while (!canFlip)
                yield return string.Format("trycancel After {0} flip(s), command execution was canceled!", x + 1);
            Switches[switchPositions[x]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    IEnumerator TwitchHandleForcedSolve()
    {
        if (moduleSolved)
            yield break;
        var curCombo = new List<List<int>> { new List<int>() };
        var foundCombos = new List<List<int>>();

        while (!foundCombos.Any() && curCombo.Any())
        {
            var nextCombos = new List<List<int>>();
            foreach (var aCombo in curCombo)
            {
                var nextOptions = Enumerable.Range(0, 5).Where(a => a != (aCombo.Any() ? aCombo.Last() : lastSwitch));
                foreach (var anOption in nextOptions)
                {
                    var createdCombo = aCombo.Concat(new[] { anOption }).ToList();
                    var simulatedSwitchPos = switchPositions.ToArray();
                    var simulatedGoalPos = goalPositions.ToArray();
                    foreach (var idx in createdCombo)
                    {
                        var idxToggleGoal = goalToggles[idx];
                        var idxToggleSwitch = switchToggles[idx];
                        simulatedSwitchPos[idx] ^= true;
                        simulatedSwitchPos[idxToggleSwitch] ^= true;
                        simulatedGoalPos[idxToggleGoal] ^= true;
                    }
                    if (simulatedSwitchPos.SequenceEqual(simulatedGoalPos))
                        foundCombos.Add(createdCombo);
                    nextCombos.Add(createdCombo);
                }
            }
            yield return true;
            curCombo = nextCombos;
        }
        Debug.LogFormat("<Red Switches #{0}> D = {1} solution found.", moduleId, foundCombos.First().Count);
        foreach (var idx in foundCombos.First())
        {
            while (!canFlip)
                yield return true;
            Switches[idx].OnInteract();
        }
    }
}