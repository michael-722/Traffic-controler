using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DebugController : MonoBehaviour
{
    private bool showConsole = false;
    private bool showHelp = true;

    private bool focusTextField = false;
    private string input;

    private int consoleHeight = 30;
    private Vector2 scrollPos;

    private string autoCompleteSuggestion = "";//current command suggestion
    private int suggestionIndex = 0;//used to cycle through multiple suggestions if there are more than one

    //command definitions

    public static DebugCommand ShowHelp;

    public List<object> commandList;

    /*
    when creating a command that needs a type not a void return (i.e. int, float, string, bool)
    must use a try and catch to handle invalid input errors 
    */

    private void Awake()
    {
        //command setup
        //command format = new DebugCommand<type>("commandId", "command description", "command format (name<T>)", (type value) => { });

        ShowHelp = new DebugCommand("showHelp", "show command list", "showHelp", () =>
        {
            showHelp = !showHelp;
        });

        //command indexing
        commandList = new List<object>
        {
            ShowHelp
        };
    }

    private void Update()
    {
        //toggle console with the ` key
        if (Input.GetKeyDown(KeyCode.BackQuote)) ToggleConsole();
    }

    public void ToggleConsole()
    {
        showConsole = !showConsole;

        if (showConsole)
        {
            focusTextField = true;//set focus next frame
        }
        else
        {
            GUI.FocusControl(null);//remove focus when closing
        }
    }

    private void OnGUI()
    {
        if (!showConsole) return;

        //check for ` key to close console
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.BackQuote)
        {
            ToggleConsole();
            return;
        }

        //check for enter key to process input
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            ToggleConsole();
            ProcessInput(input);
            input = "";//clear input after execution
            return;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
        {
            HandleAutoComplete();
            Event.current.Use();//stop tab from focusing next control
        }

        float y = Screen.height - consoleHeight;

        if (showHelp)//draw help box
        {
            GUI.Box(new Rect(0, y - 100, Screen.width / 2, 100), "");
            Rect viewport = new Rect(0, 0, Screen.width - 30, 20 * commandList.Count);

            scrollPos = GUI.BeginScrollView(new Rect(0, y - 100, Screen.width, 90), scrollPos, viewport);

            for (int i = 0; i < commandList.Count; i++)
            {
                BaseDebugCommand command = commandList[i] as BaseDebugCommand;

                string lable = $"{command.commandFormat} - {command.commandDescription}";
                Rect lableRect = new Rect(5, i * 20, viewport.width - 10, 20);

                GUI.Label(lableRect, lable);
            }

            GUI.EndScrollView();
        }

        //draw console background and text field
        GUI.Box(new Rect(0, y, Screen.width / 2, 30), "");
        GUI.SetNextControlName("ConsoleInput");
        input = GUI.TextField(new Rect(10, y + 5, (Screen.width / 2) - 20, 20), input);

        UpdateAutoCompleteSuggestion();//update suggestion based on current input

        if (focusTextField)//force focus on txt field when console opens
        {
            GUI.FocusControl("ConsoleInput");
            focusTextField = false;
        }

        //auto complete suggestion display
        if (!string.IsNullOrEmpty(autoCompleteSuggestion) && autoCompleteSuggestion.StartsWith(input) && input.Length > 0)
        {
            var textSize = GUI.skin.textField.CalcSize(new GUIContent(input));
            GUI.color = new Color(1f, 1f, 1f, 0.3f);//make suggestion faint
            GUI.Label(new Rect(10 + textSize.x + 2, y + 5, 300, 20), autoCompleteSuggestion.Substring(input.Length));
            GUI.color = Color.white;
        }
    }

    private void ProcessInput(string input)
    {
        string[] properties = input.Split(' ');//split input into properties

        for (int i = 0; i < commandList.Count; i++)
        {
            BaseDebugCommand commandBase = commandList[i] as BaseDebugCommand;//cast to base to access commandId

            //command execution depends on type provided
            if (input.Contains(commandBase.commandId)) // check if input contains command id
            {
                try
                {
                    switch (commandList[i])
                    {
                        case DebugCommand cmd:
                            cmd.Execute();
                            break;

                        case DebugCommand<int> cmdInt:
                            cmdInt.Execute(int.Parse(properties[1]));
                            break;

                        case DebugCommand<float> cmdFloat:
                            cmdFloat.Execute(float.Parse(properties[1]));
                            break;

                        case DebugCommand<string> cmdString:
                            cmdString.Execute(properties[1]);
                            break;

                        case DebugCommand<bool> cmdBool:
                            cmdBool.Execute(bool.Parse(properties[1]));
                            break;

                        default:
                            Debug.LogError("command not found or invalid command format");
                            break;
                    }
                }
                catch
                {
                    Debug.LogError("invalid command parameters");
                }
            }
        }
    }

    //auto complete handling
    private void UpdateAutoCompleteSuggestion()
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            autoCompleteSuggestion = "";
            return;
        }

        //get all possible matches based on what the user has typed
        var matches = commandList
            .Select(c => (c as BaseDebugCommand).commandId)
            .Where(id => id.StartsWith(input, System.StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count > 0)
        {
            autoCompleteSuggestion = matches[suggestionIndex % matches.Count];
        }
        else
        {
            autoCompleteSuggestion = "";
        }
    }

    private void HandleAutoComplete()
    {
        if (string.IsNullOrEmpty(autoCompleteSuggestion)) return;

        input = autoCompleteSuggestion;//fill in the suggestion

        suggestionIndex++;//move to next match if there are multiple

        //force gui to refresh with new value
        GUI.FocusControl("ConsoleInput");

        //set text cursor to end of input
        TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        if (te != null)
        {
            te.text = input;
            te.cursorIndex = input.Length;
            te.selectIndex = input.Length;//remove any selection and place caret at end
        }
    }
}
