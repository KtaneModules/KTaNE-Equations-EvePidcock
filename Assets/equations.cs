using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KMHelper;
using UnityEngine;
using Random = UnityEngine.Random;

public class equations : MonoBehaviour {
    private static int _moduleIdCounter = 1;

    private static readonly double nothing = -1000000.17;
    private bool _isSolved, _lightsOn, isEnd;
    private int _moduleId;

    private double a, b, c, d, correctNumberShort, decimalIndex = 10, correctNumber, displayNumber = nothing;

    private int blueCount, redCount, pinkCount, yellowCount, greenCount, LEDsOn;

    public KMSelectable clear, submit, deci, negative;

    private bool decimalAlone, decimalActive, usingX, strikeCo, negAlone;
    public KMBombInfo info;

    public Material[] keyColors; //0=blue, 1=red, 2=pink, 3=yellow, 4=green
    public KMSelectable[] keys;

    public Material LEDOn, LEDOff;

    public MeshRenderer[] leds;
    public KMBombModule module;
    public KMAudio newAudio;

    public TextMesh[] cbText;
    public GameObject cbObj;

    private int oneColor, fiveColor, nineColor;

    public TextMesh screenText;

    private void Start() {
        _moduleId = _moduleIdCounter++;
        module.OnActivate += Activate;
    }

    private void Activate() {
        Init();
        _lightsOn = true;
    }

    private void Awake() {
        clear.OnInteract += delegate {
            handlePressClear();
            return false;
        };
        submit.OnInteract += delegate {
            handlePressSubmit();
            return false;
        };
        deci.OnInteract += delegate {
            handleDecimalPress();
            return false;
        };
        negative.OnInteract += delegate {
            handleNegativePress();
            return false;
        };
        for (double i = 0; i < 10; i++) {
            var j = i;
            keys[(int) i].OnInteract += delegate {
                handleKeyPress(j);
                return false;
            };
        }
    }

    private void handlePressClear() {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, clear.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        displayNumber = nothing;
        decimalActive = false;
        decimalAlone = false;
        decimalIndex = 10;
        negAlone = false;
        Debug.LogFormat("[Equations #{0}] Clear pressed.", _moduleId);
        RenderScreen();
    }

    private void handlePressSubmit() {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        var input = displayNumber;
        if (input == correctNumber || input == correctNumberShort || Math.Abs(input-correctNumber)<=0.1 || (decimalIndex >= 10000 && correctNumber.ToString().StartsWith(input.ToString()))) {
            isEnd = true;
            strikeCo = false;
            StartCoroutine(end());
            Debug.LogFormat("[Equations #{0}] Solved with answer: " + (input == nothing ? "-blank screen-" : "{1}") + ".", _moduleId, input);
        } else {
            isEnd = true;
            StartCoroutine(end());
            strikeCo = true;
            Debug.LogFormat("[Equations #{0}] Inputed answer: " + (input == nothing ? "-blank screen-" : "{1}") + ", correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{2}."), _moduleId, input.ToString(), correctNumber.ToString());
            Debug.LogFormat("[Equations #{0}] If you feel that this is a mistake, please do not hesitate to contact @AAces#0908 on discord so we can get this sorted out. Be sure to have a copy of this log file handy.", _moduleId);
        }
    }

    private void handleDecimalPress() {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, deci.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        if (!decimalActive)
            if (!decimalAlone) {
                decimalAlone = true;
                decimalActive = true;
                if (displayNumber == nothing) displayNumber = 0;
            }

        RenderScreen();
    }

    private void handleNegativePress() {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, negative.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        if (displayNumber == nothing || displayNumber == 0)
            negAlone = !negAlone;
        else
            displayNumber *= -1;
        RenderScreen();
    }

    private void handleKeyPress(double key) {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keys[(int) key].transform);
        if (!_lightsOn || _isSolved || isEnd) return;

        if (!decimalActive) {
            if (displayNumber == nothing) displayNumber = 0;
            displayNumber *= 10;
            if (displayNumber < 0 || negAlone)
                displayNumber -= key;
            else
                displayNumber += key;
        } else {
            if (decimalAlone) decimalAlone = false;
            if (displayNumber >= 0 && !negAlone)
                displayNumber += key / decimalIndex;
            else
                displayNumber -= key / decimalIndex;
            decimalIndex *= 10;
        }

        if (negAlone && key != 0) negAlone = false;
        RenderScreen();
    }

    private void Init() {
        screenText.color = new Color(255, 255, 255);
        var rand = Random.Range(0, 2);
        if (rand == 1) {
            leds[0].material = LEDOn;
            LEDsOn++;
        } else {
            leds[0].material = LEDOff;
        }

        rand = Random.Range(0, 2);
        if (rand == 1) {
            leds[1].material = LEDOn;
            LEDsOn++;
        } else {
            leds[1].material = LEDOff;
        }

        rand = Random.Range(0, 2);
        if (rand == 1) {
            leds[2].material = LEDOn;
            LEDsOn++;
        } else {
            leds[2].material = LEDOff;
        }

        Debug.LogFormat("[Equations #{0}] {1} LEDs are on.", _moduleId, LEDsOn);
        setupKeyColors();
        RenderScreen();
        setupStaticVariables();
        Debug.LogFormat("[Equations #{0}] Static variables: a = {1}, b = {2}, c = {3}, d = {4}.", _moduleId, a, b, c, d);
        getCorrectAnswer();
        if (usingX)
            Debug.LogFormat("[Equations #{0}] Solve for variable: x.", _moduleId);
        else
            Debug.LogFormat("[Equations #{0}] Solve for variable: y.", _moduleId);
        if (correctNumber.ToString().Contains(".")) {
            if (correctNumber.ToString().Split('.')[1].Length > 3) {
                correctNumberShort = double.Parse(correctNumber.ToString().Split('.')[0] + "." + correctNumber.ToString().Split('.')[1].Substring(0, 3));
                Debug.LogFormat("[Equations #{0}] Correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{1}."), _moduleId, correctNumberShort.ToString());
            } else {
                Debug.LogFormat("[Equations #{0}] Correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{1}."), _moduleId, correctNumber.ToString());
            }
        } else {
            Debug.LogFormat("[Equations #{0}] Correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{1}."), _moduleId, correctNumber.ToString());
        }
        cbObj.SetActive(false);
        if (GetComponent<KMColorblindMode>().ColorblindModeActive) {
            cbObj.SetActive(true);
        }
    }

    private void RenderScreen() {
        if (negAlone) {
            screenText.text = "-";
            return;
        }

        if (displayNumber == nothing) {
            screenText.text = "";
            return;
        }

        if (decimalAlone)
            screenText.text = displayNumber + ".";
        else
            screenText.text = displayNumber.ToString();
    }

    private void setupKeyColors() {
        var x = 0;
        foreach (var key in keys) {
            var color = Random.Range(0, 5);
            if (x == 1)
                oneColor = color;
            else if (x == 5)
                fiveColor = color;
            else if (x == 9) nineColor = color;
            key.GetComponentInParent<MeshRenderer>().material = keyColors[color];
            switch (color) //0=blue, 1=red, 2=pink, 3=yellow, 4=green
            {
                case 0:
                    blueCount++;
                    cbText[x].text = "B";
                    break;
                case 1:
                    redCount++;
                    cbText[x].text = "R";
                    break;
                case 2:
                    pinkCount++;
                    cbText[x].text = "P";
                    break;
                case 3:
                    yellowCount++;
                    cbText[x].text = "Y";
                    break;
                case 4:
                    greenCount++;
                    cbText[x].text = "G";
                    break;
            }

            x++;
        }

        Debug.LogFormat("[Equations #{0}] There are {1} blue keys, {2} red keys, {3} pink keys, {4} yellow keys, and {5} green keys.", _moduleId, blueCount, redCount, pinkCount, yellowCount, greenCount);
    }

    private void setupStaticVariables() {
        a = info.GetOnIndicators().Count() % 3;
        var bTemp = info.GetSerialNumberNumbers().ToArray();
        foreach (var i in bTemp) b += i;
        b -= 2 * LEDsOn;
        c = DateTime.Today.Month + (3 - LEDsOn);
        d = blueCount * (redCount - yellowCount);
    }

    private void getCorrectAnswer() {
        usingX = (info.GetIndicators().Count() + LEDsOn) % 4 < 2;
        var bTemp = info.GetSerialNumberNumbers().ToArray();
        var x = 0;
        foreach (var i in bTemp) x += i;
        if (info.GetOnIndicators().Count() > 2) {
            correctNumber = systemAnswer(2);
            Debug.LogFormat("[Equations #{0}] Using system #2", _moduleId);
        } else if (oneColor == fiveColor && oneColor == nineColor) {
            correctNumber = systemAnswer(5);
            Debug.LogFormat("[Equations #{0}] Using system #5", _moduleId);
        } else if (pinkCount > greenCount) {
            correctNumber = systemAnswer(3);
            Debug.LogFormat("[Equations #{0}] Using system #3", _moduleId);
        } else if (LEDsOn > 1) {
            correctNumber = systemAnswer(1);
            Debug.LogFormat("[Equations #{0}] Using system #1", _moduleId);
        } else if (x >= 16) {
            correctNumber = systemAnswer(6);
            Debug.LogFormat("[Equations #{0}] Using system #6", _moduleId);
        } else {
            correctNumber = systemAnswer(4);
            Debug.LogFormat("[Equations #{0}] Using system #4", _moduleId);
        }
    }

    private double systemAnswer(int system) {
        double x;
        double y;
        switch (system) {
            case 1:
                if (2 * a - b == 0) return nothing;
                x = c / (2 * a - b);
                y = (a - b) * x / 2;
                if (usingX)
                    return x;
                else
                    return y;
            case 2:
                x = d - c;
                if (a == 0) return nothing;
                y = (d - c + d) / a;
                if (usingX)
                    return x;
                else
                    return y;
            case 3:
                if (b == 0 || b == 1) return nothing;
                x = (a + c) / (b - 1);
                y = (x + a) / b;
                if (usingX)
                    return x;
                else
                    return y;
            case 4:
                y = b - a - c;
                x = -b + 2 * a + 2 * c;
                if (usingX)
                    return x;
                else
                    return y;
            case 5:
                if (c == -2) return nothing;
                x = (2 * a - 2 * c * d) / (2 + c);
                y = 2 * d + x;
                if (usingX)
                    return x;
                else
                    return y;
            case 6:

                if (a == 0 && b != 0) {
                    x = d / b;
                    y = c / b;
                    if (usingX)
                        return x;
                    return y;
                } else if (a != 0 && b == 0) {
                    x = c / a;
                    y = d / a;
                    if (usingX)
                        return x;
                    return y;
                } else if (b * b - a * a == 0 && a == 0) {
                    return nothing;
                }

                y = (b * c - a * d) / (b * b - a * a);
                x = (c - b * y) / a;
                if (usingX)
                    return x;
                else
                    return y;
        }

        return nothing;
    }

    private IEnumerator end() {
        var neg = false;
        if (displayNumber != nothing) {
            if (displayNumber < 0)
                if (displayNumber != nothing) {
                    displayNumber *= -1;
                    RenderScreen();
                    neg = true;
                }

            var text = displayNumber.ToString();
            for (var i = 0; i < text.Length; i++) {
                screenText.text = text.Substring(0, text.Length - i);
                yield return new WaitForSeconds(0.1f);
            }
        }

        screenText.text = "";
        yield return new WaitForSeconds(0.25f);
        var tempText = "0";
        while (tempText != "00000000000000000") {
            screenText.text = tempText;
            tempText = tempText + "0";
            yield return new WaitForSeconds(0.1f);
        }

        if (neg) displayNumber *= -1;
        if (strikeCo) {
            module.HandleStrike();
            screenText.color = new Color(255, 0, 0);
            displayNumber = nothing;
            decimalActive = false;
            decimalAlone = false;
            decimalIndex = 10;
            RenderScreen();
        } else {
            module.HandlePass();
            screenText.color = new Color(0, 255, 0);
            newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, submit.transform);
            _isSolved = true;
        }

        screenText.text = "";
        yield return new WaitForSeconds(0.15f);
        screenText.text = "0000000000000000";
        yield return new WaitForSeconds(0.15f);
        screenText.text = "";
        yield return new WaitForSeconds(0.15f);
        screenText.text = "0000000000000000";
        yield return new WaitForSeconds(0.15f);
        screenText.text = "";
        yield return new WaitForSeconds(0.15f);
        screenText.text = "0000000000000000";
        yield return new WaitForSeconds(0.15f);
        displayNumber = nothing;
        decimalActive = false;
        decimalAlone = false;
        decimalIndex = 10;
        RenderScreen();
        if (!strikeCo) {
            leds[0].material = LEDOff;
            leds[1].material = LEDOff;
            leds[2].material = LEDOff;
        }

        screenText.color = new Color(255, 255, 255);
        isEnd = false;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} submit -4.82' to submit -4.82 as your answer. Use '!{0} clear' to clear the screen.";
#pragma warning restore 414

    protected KMSelectable[] ProcessTwitchCommand(string input) {
        var buttons = new Dictionary<char, KMSelectable> {
            {'0', keys[0]},
            {'1', keys[1]},
            {'2', keys[2]},
            {'3', keys[3]},
            {'4', keys[4]},
            {'5', keys[5]},
            {'6', keys[6]},
            {'7', keys[7]},
            {'8', keys[8]},
            {'9', keys[9]},
            {'-', negative},
            {'.', deci}
        };

        if (input.Trim().ToLowerInvariant() == "colorblind") {
            cbObj.SetActive(true);
            return new KMSelectable[0];
        }

        var split = input.Trim().ToLowerInvariant().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

        if (split.Length == 1 && split[0] == "submit") return new[] {submit};
        if (split.Length == 1 && split[0] == "clear") return new[] {clear};

        if (split.Length == 2 && split[0] == "submit" && Regex.IsMatch(split[1], @"^[-.0-9]+$")) {
            handlePressClear();
            return split[1].Select(c => buttons[c]).Concat(new[] {submit}).ToArray();
        }

        if (split.Length == 1 && Regex.IsMatch(split[0], @"^[-.0-9]+$")) {
            handlePressClear();
            return split[0].Select(c => buttons[c]).ToArray();
        }

        return null;
    }
}