using System;

//base class for commands
public class BaseDebugCommand {
    private string _commandId;
    private string _commandDescription;
    private string _commandFormat;

    //readonly properties
    public string commandId { get { return _commandId; } }
    public string commandDescription { get { return _commandDescription; } }
    public string commandFormat { get { return _commandFormat; } }

    //constructor
    public BaseDebugCommand(string id, string description, string format) {
        _commandId = id;
        _commandDescription = description;
        _commandFormat = format;
    }
}

//command with no parameters
public class DebugCommand : BaseDebugCommand {
    private Action command;

    public DebugCommand(string id, string description, string format, Action command) : base(id, description, format) {
        this.command = command;
    }

    public void Execute() {
        command?.Invoke();
    }
}

//command with parameter
public class DebugCommand<T1> : BaseDebugCommand
{
    private Action<T1> command;

    public DebugCommand(string id, string description, string format, Action<T1> command) : base(id, description, format) {
        this.command = command;
    }

    public void Execute(T1 value) {
        command?.Invoke(value);
    }
}