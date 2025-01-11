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
    }

    // TP and autosolver written by Quinn Wuest
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = @"!{0} 1 2 3 4 5 [Flip switches 1, 2, 3, 4, 5.] | Switches are numbered from left to right.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command) {
        var parameters = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (parameters.Length == 0)
            yield break;

        var skip = (new[] { "flip", "press", "switch", "toggle" }).Contains(parameters[0]) ? 1 : 0;

        if (parameters.Skip(skip).Any(i => { int val; return !int.TryParse(i.Trim(), out val) || val < 1 || val > 5; }))
            yield break;

        yield return null;

        foreach(var p in parameters.Skip(skip)) {
            var sw = int.Parse(p.Trim()) - 1;
            while (!canFlip)
                yield return null;
            Switches[sw].OnInteract();
            yield return new WaitForSeconds(0.25f);
        }
    }

    public class SwitchState {
        public bool[] SwitchPositions { get; private set; }
        public bool[] LedPositions { get; private set; }
        public int PreviousSwitch { get; private set; }

        public SwitchState(bool[] switchPositions, bool[] ledPositions, int prevSwitch) {
            SwitchPositions = switchPositions;
            LedPositions = ledPositions;
            PreviousSwitch = prevSwitch;
        }
    }

    public SwitchState GetSwitchState(SwitchState state, int sw) {
        bool[] swPos = state.SwitchPositions.ToArray();
        bool[] ledPos = state.LedPositions.ToArray();
        swPos[sw] = !swPos[sw];
        swPos[switchToggles[sw]] = !swPos[switchToggles[sw]];
        ledPos[goalToggles[sw]] = !ledPos[goalToggles[sw]];

        return new SwitchState(swPos, ledPos, sw);
    }

    struct QueueItem {
        public SwitchState State { get; private set; }
        public SwitchState Parent { get; private set; }
        public int Action { get; private set; }
        
        public QueueItem(SwitchState state, SwitchState parent, int action) {
            State = state;
            Parent = parent;
            Action = action;
        }
    }

    private IEnumerator TwitchHandleForcedSolve() {
        var currentState = new SwitchState(switchPositions, goalPositions, lastSwitch);

        var visited = new Dictionary<SwitchState, QueueItem>();
        var q = new Queue<QueueItem>();
        q.Enqueue(new QueueItem(currentState, null, 0));

        SwitchState solutionState = null;
        while(q.Count > 0) {
            var qi = q.Dequeue();
            if (visited.ContainsKey(qi.State))
                continue;
            visited[qi.State] = qi;
            if (qi.State.SwitchPositions.SequenceEqual(qi.State.LedPositions)) {
                solutionState = qi.State;
                break;
            }
            if (qi.State.PreviousSwitch != 0)
                q.Enqueue(new QueueItem(GetSwitchState(qi.State, 0), qi.State, 0));
            if (qi.State.PreviousSwitch != 1)
                q.Enqueue(new QueueItem(GetSwitchState(qi.State, 1), qi.State, 1));
            if (qi.State.PreviousSwitch != 2)
                q.Enqueue(new QueueItem(GetSwitchState(qi.State, 2), qi.State, 2));
            if (qi.State.PreviousSwitch != 3)
                q.Enqueue(new QueueItem(GetSwitchState(qi.State, 3), qi.State, 3));
            if (qi.State.PreviousSwitch != 4)
                q.Enqueue(new QueueItem(GetSwitchState(qi.State, 4), qi.State, 4));
        }

        var r = solutionState;
        var path = new List<int>();
        while (true) {
            var nr = visited[r];
            if (nr.Parent == null)
                break;
            path.Add(nr.Action);
            r = nr.Parent;
        }

        for (int i = path.Count - 1; i >= 0; i--) {
            while (!canFlip)
                yield return null;
            Switches[path[i]].OnInteract();
            if (!moduleSolved)
                yield return new WaitForSeconds(0.25f);
        }
    }
}