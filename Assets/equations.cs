using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using KMHelper;
using System;
using Random = UnityEngine.Random;
using UnityEngine;

public class equations : MonoBehaviour {

    private static int _moduleIdCounter = 1;
    public KMAudio newAudio;
    public KMBombModule module;
    public KMBombInfo info;
    private int _moduleId = 0;
    private bool _isSolved = false, _lightsOn = false, isEnd = false;

    public KMSelectable clear, submit, deci, plusminus;
    public KMSelectable[] keys;

    private bool decimalAlone, decimalActive, usingX, strikeCo = false;

    private static double nothing = -1000000.17;

    private double a, b, c, d, correctNumberShort, decimalIndex = 10, correctNumber, displayNumber = nothing;

    public Material[] keyColors; //0=blue, 1=red, 2=pink, 3=yellow, 4=green

    private int blueCount, redCount, pinkCount, yellowCount, greenCount, LEDsOn;

    public MeshRenderer[] leds;

    public Material LEDOn, LEDOff;

    public TextMesh screenText;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        module.OnActivate += Activate;
    }

    void Activate()
    {
        Init();
        _lightsOn = true;
    }

    private void Awake()
    {
        clear.OnInteract += delegate ()
        {
            handlePressClear();
            return false;
        };
        submit.OnInteract += delegate ()
        {
            handlePressSubmit();
            return false;
        };
        deci.OnInteract += delegate ()
        {
            handleDecimalPress();
            return false;
        };
        plusminus.OnInteract += delegate ()
        {
            handlePlusMinusPress();
            return false;
        };
        for (double i = 0; i < 10; i++)
        {
            double j = i;
            keys[(int)i].OnInteract += delegate ()
            {
                handleKeyPress(j);
                return false;
            };
        }
    }
    
    void handlePressClear()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, clear.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        displayNumber = nothing;
        decimalActive = false;
        decimalAlone = false;
        decimalIndex = 10;
        Debug.LogFormat("[Equations #{0}] Clear pressed.", _moduleId);
        RenderScreen();
    }

    void handlePressSubmit()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        if (displayNumber == correctNumber || displayNumber == correctNumberShort || (decimalIndex>=10000 && correctNumber.ToString().StartsWith(displayNumber.ToString())))
        {
            isEnd = true;
            strikeCo = false;
            StartCoroutine(end());
            Debug.LogFormat("[Equations #{0}] Solved with answer: "+(displayNumber == nothing ? "-blank screen-" : "{1}")+".", _moduleId, displayNumber);

        } else
        {
            isEnd = true;
            StartCoroutine(end());
            strikeCo = true;
            Debug.LogFormat("[Equations #{0}] Inputed answer: " + (displayNumber == nothing ? "-blank screen-" : "{1}") + ", correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{2}."), _moduleId, displayNumber.ToString(), correctNumber.ToString());
            Debug.LogFormat("[Equations #{0}] If you feel that this is a mistake, please do not hesitate to contact @AAces#0908 on discord so we can get this sorted out. Be sure to have a copy of this log file handy.", _moduleId);

        }
    }

    void handleDecimalPress()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, deci.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        if (!decimalActive)
        {
            if (!decimalAlone)
            {
                decimalAlone = true;
                decimalActive = true;
                if (displayNumber == nothing)
                {
                    displayNumber = 0;
                }
            }
        }
        Debug.LogFormat("[Equations #{0}] Decimal pressed. Number displayed is now {1}.", _moduleId, displayNumber);
        RenderScreen();
    }

    void handlePlusMinusPress()
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, plusminus.transform);
        if (!_lightsOn || _isSolved || isEnd) return;
        if (displayNumber == nothing)
        {
            return;
        }
        displayNumber *= -1;
        RenderScreen();
        Debug.LogFormat("[Equations #{0}] Plus/Minus pressed. Number displayed is now {1}.", _moduleId, displayNumber);
    }

    void handleKeyPress(double key)
    {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keys[(int)key].transform);
        if (!_lightsOn || _isSolved || isEnd) return;

        if (!decimalActive)
        {
            if (displayNumber == nothing)
            {
                displayNumber = 0;
            }
            displayNumber *= 10;
            displayNumber += key;
        } else
        {
            if (decimalAlone)
            {
                decimalAlone = false;
            }
            if (displayNumber >= 0)
            {
                displayNumber += (key / decimalIndex);
            } else
            {
                displayNumber -= (key / decimalIndex);
            }
            decimalIndex *= 10;
        }
        Debug.LogFormat("[Equations #{0}] {1} pressed. Number displayed is now {2}.", _moduleId, key, displayNumber);
        RenderScreen();
    }

    void Init()
    {
        screenText.color = new Color(255, 255, 255);
        int rand = Random.Range(0,2);
        if(rand == 1)
        {
            leds[0].material = LEDOn;
            LEDsOn++;
        } else
        {
            leds[0].material = LEDOff;
        }
        rand = Random.Range(0, 2);
        if (rand == 1)
        {
            leds[1].material = LEDOn;
            LEDsOn++;
        }
        else
        {
            leds[1].material = LEDOff;
        }
        rand = Random.Range(0, 2);
        if (rand == 1)
        {
            leds[2].material = LEDOn;
            LEDsOn++;
        }
        else
        {
            leds[2].material = LEDOff;
        }
        setupKeyColors();
        RenderScreen();
        setupStaticVariables();
        Debug.LogFormat("[Equations #{0}] Static variables: a = {1}, b = {2}, c = {3}, d = {4}.", _moduleId, a, b, c, d);
        getCorrectAnswer();
        if (usingX)
        {
            Debug.LogFormat("[Equations #{0}] Solve for variable: x.", _moduleId);
        }
        else
        {
            Debug.LogFormat("[Equations #{0}] Solve for variable: y.", _moduleId);
        }
        if (correctNumber.ToString().Contains("."))
        {
            if (correctNumber.ToString().Split('.')[1].Length > 3)
            {
                correctNumberShort = Double.Parse(correctNumber.ToString().Split('.')[0] + "." + correctNumber.ToString().Split('.')[1].Substring(0, 3));
                Debug.LogFormat("[Equations #{0}] Correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{1}."), _moduleId, correctNumberShort.ToString());
            }
            else
            {
                Debug.LogFormat("[Equations #{0}] Correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{1}."), _moduleId, correctNumber.ToString());
            }
        }
        else
        {
            Debug.LogFormat("[Equations #{0}] Correct answer: " + (correctNumber == nothing ? "-blank screen, system has no solution-." : "{1}."), _moduleId, correctNumber.ToString());
        }
    }

    void RenderScreen()
    {
        if(displayNumber == nothing)
        {
            screenText.text = "";
            return;
        }
        if (decimalAlone)
        {
            screenText.text = displayNumber.ToString() + ".";
        }
        else
        {
            screenText.text = displayNumber.ToString();
        }
        
    }

    private int oneColor, fiveColor, nineColor;

    void setupKeyColors()
    {
        int x = 0;
        foreach(KMSelectable key in keys)
        {
            int color = Random.Range(0, 5);
            if (x == 1)
            {
                oneColor = color;
            } else if(x == 5)
            {
                fiveColor = color;
            }else if(x == 9)
            {
                nineColor = color;
            }
            key.GetComponentInParent<MeshRenderer>().material = keyColors[color];
            switch (color) //0=blue, 1=red, 2=pink, 3=yellow, 4=green
            {
                case 0:
                    blueCount++;
                    break;
                case 1:
                    redCount++;
                    break;
                case 2:
                    pinkCount++;
                    break;
                case 3:
                    yellowCount++;
                    break;
                case 4:
                    greenCount++;
                    break;

            }
            x++;
        }
        Debug.LogFormat("[Equations #{0}] There are {1} blue keys, {2} red keys, {3} pink keys, {4} yellow keys, and {5} green keys.", _moduleId, blueCount, redCount, pinkCount, yellowCount, greenCount);
    }

    void setupStaticVariables()
    {
        a = (info.GetOnIndicators().Count() % 3);
        int[] bTemp = info.GetSerialNumberNumbers().ToArray();
        foreach(int i in bTemp)
        {
            b += i;
        }
        b -= (2 * LEDsOn);
        c = DateTime.Today.Month + (3-LEDsOn);
        d = blueCount * (redCount - yellowCount); 
    }

    void getCorrectAnswer()
    {
        usingX = (((info.GetIndicators().Count() + LEDsOn) % 4 ) < 2);
        int[] bTemp = info.GetSerialNumberNumbers().ToArray();
        int x = 0;
        foreach (int i in bTemp)
        {
            x += i;
        }
        if (info.GetOnIndicators().Count() > 2)
        {
            correctNumber = systemAnswer(2);
            Debug.LogFormat("[Equations #{0}] Using system #2", _moduleId);
        } else if (oneColor==fiveColor && oneColor==nineColor)
        {
            correctNumber = systemAnswer(5);
            Debug.LogFormat("[Equations #{0}] Using system #5", _moduleId);
        } else if(pinkCount > greenCount)
        {
            correctNumber = systemAnswer(3);
            Debug.LogFormat("[Equations #{0}] Using system #3", _moduleId);
        } else if(LEDsOn > 1)
        {
            correctNumber = systemAnswer(1);
            Debug.LogFormat("[Equations #{0}] Using system #1", _moduleId);
        } else if(x>=16)
        {
            correctNumber = systemAnswer(6);
            Debug.LogFormat("[Equations #{0}] Using system #6", _moduleId);
        } else
        {
            correctNumber = systemAnswer(4);
            Debug.LogFormat("[Equations #{0}] Using system #4", _moduleId);
        }
    }

    double systemAnswer(int system)
    {
        double x;
        double y;
        switch (system)
        {

            case 1:
                if (((2 * a) - b) == 0)
                {
                    return nothing;
                }
                x = (c / ((2 * a) - b));
                y = (((a-b)*x)/2);
                if (usingX)
                {
                    return x;
                } else
                {
                    return y;
                }
            case 2:
                x = (d - c);
                if (a == 0 && !usingX) { return nothing; }
                y = ((d - c + d) / a);
                if (usingX)
                {
                    return x;
                }
                else
                {
                    return y;
                }
            case 3:
                if (b == 0 || b == 1) { return nothing; }
                x = ((a + c) / (b - 1));
                y = ((x + a) / b);
                if (usingX)
                {
                    return x;
                }
                else
                {
                    return y;
                }
            case 4:
                y = b - a - c;
                x = (-b) + (2 * a) + (2 * c);
                if (usingX)
                {
                    return x;
                }
                else
                {
                    return y;
                }
            case 5:
                if (c == -2) { return nothing; }
                x = (((2 * a)-(2*c*d))/(2+c));
                y = (2 * d) + x;
                if (usingX)
                {
                    return x;
                }
                else
                {
                    return y;
                }
            case 6:

                if(a==0 && b != 0)
                {
                    x = d / b;
                    y = c / b;
                    if (usingX)
                    {
                        return x;
                    }
                    else
                    {
                        return y;
                    }
                } else if(a!=0 && b==0){
                    x = c / a;
                    y = d / a;
                    if (usingX)
                    {
                        return x;
                    }
                    else
                    {
                        return y;
                    }
                } else

                if (((b * b) - (a * a)) == 0 && a==0) { return nothing; }
                y = (((b * c) - (a * d)) / ((b * b) - (a * a)));
                x = ((c - (b * y)) / (a));
                if (usingX)
                {
                    return x;
                }
                else
                {
                    return y;
                }
        }
        return nothing;
    }

    private IEnumerator end()
    {
        bool neg = false;
        if (displayNumber != nothing)
        {
            if (displayNumber < 0)
            {
                if (displayNumber != nothing)
                {
                    displayNumber *= -1;
                    RenderScreen();
                    neg = true;
                }
            }
            string text = displayNumber.ToString();
            for (int i = 0; i < text.Length; i++)
            {
                screenText.text = text.Substring(0, text.Length - i);
                yield return new WaitForSeconds(0.1f);
            }
        }
        screenText.text = "";
        yield return new WaitForSeconds(0.25f);
        string tempText = "0";
        while (tempText != "00000000000000000")
        {
            screenText.text = tempText;
            tempText = tempText + "0";
            yield return new WaitForSeconds(0.1f);
        }
        if (neg)
        {
            displayNumber *= -1;
        }
        if (strikeCo)
        {
            module.HandleStrike();
            screenText.color = new Color(255,0,0);
            displayNumber = nothing;
            decimalActive = false;
            decimalAlone = false;
            decimalIndex = 10;
            RenderScreen();
        } else
        {
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
        if (!strikeCo)
        {
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
    protected KMSelectable[] ProcessTwitchCommand(string input)
    {
        Dictionary<char, KMSelectable> buttons = new Dictionary<char, KMSelectable>()
        {
            { '0', keys[0] },
            { '1', keys[1] },
            { '2', keys[2] },
            { '3', keys[3] },
            { '4', keys[4] },
            { '5', keys[5] },
            { '6', keys[6] },
            { '7', keys[7] },
            { '8', keys[8] },
            { '9', keys[9] },
            { '-', plusminus },
            { '.', deci }        
        };

        var split = input.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (split.Length == 1 && split[0] == "submit") { return new KMSelectable[] { submit }; }
        if (split.Length == 1 && split[0] == "clear") { return new KMSelectable[] { clear }; }
        
        if (split.Length == 2 && split[0] == "submit" && Regex.IsMatch(split[1], @"^[-.0-9]+$")) {

            if (split[1].StartsWith("-"))
            {
                return split[1].Select(c => buttons[c]).Concat(new[] { plusminus }).Concat(new[] { submit }).ToArray();
            } else
            {
                return split[1].Select(c => buttons[c]).Concat(new[] { submit }).ToArray();
            }
        }
        if (split.Length == 1 && Regex.IsMatch(split[0], @"^[-.0-9]+$")) {
            if (split[0].StartsWith("-"))
            {
                return split[0].Select(c => buttons[c]).Concat(new[] { plusminus }).ToArray();
            }
            else
            {
                return split[0].Select(c => buttons[c]).ToArray();
            }
        }
        return null;
    }
}
