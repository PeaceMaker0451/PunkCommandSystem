# Punk Command System

## Overview

The Punk Command System is a versatile command management library that allows you to define and execute commands with custom parameters. It supports various parameter types and can handle nested commands, making it a powerful tool for creating interactive command-line applications.

## Features

- **Command Definition**: Easily define commands with specified parameter types and actions.
- **Custom Parameter Types**: Register your own parameter types for enhanced functionality.
- **Nested Commands**: Execute commands within other commands using a simple syntax.
- **Error Handling**: Comprehensive error messages for parameter mismatches and missing parameters.

## Built-in Parameter Types

The library provides several predefined parameter types that can be used when creating commands:

- **`IntParameterType`**: Accepts integer values (e.g., `5`, `-10`).
- **`FloatParameterType`**: Supports floating-point numbers (e.g., `3,14`, `-1,5`).
- **`StringParameterType`**: Supports floating-point numbers (e.g., `"Hello"`).
  
### Example of using built-in parameter types:

```csharp
var exampleCommand = new Command(
    _name: "example",
    _action: ExampleCommandAction,
    _parameters: new List<RequiredParameter>
    {
        new RequiredParameter(new IntParameterType(), "Number of repetitions"),
        new RequiredParameter(new StringParameterType(), "Message")
    }
);
commandManager.AddCommand(exampleCommand);
```

### 2. **Command Constructor Parameters**
Commands are created using a constructor that accepts several parameters. Here’s a list of parameters that can be specified when creating a command:

Пример:

```markdown
## Command Constructor Parameters

Commands are created using a constructor that accepts several parameters. Here’s a list of parameters that can be specified when creating a command:

- **`_name` (string)**: The name of the command, used to invoke it in the terminal.
- **`_action` (CommandAction)**: The logic of the command — the function that will be executed when it is called. It accepts `ParametersData`, containing the values of the parameters.
- **`_parameters` (List<RequiredParameter>)**: A list of required parameters for the command. Each parameter is specified by its data type (e.g., `IntParameterType`) and a description. 
- **`_description` (string)**: A description of the command (empty string by default). It can be used to display help or documentation.
- **`_runOtherCommandMethod` (RunOtherCommandMethod)**: A method for running other commands from the current one (default is `null`). You can use `CommandManager.ExecuteCommandAsync` or custom function.
- **`_maxCommandDepth` (int)**: (int): The maximum command nesting depth (default is `3`), limiting the number of commands that can be called within one command.
```
```csharp
// Example of creating a command:
var exampleCommand = new Command(
    _name: "example",
    _action: ExampleCommandAction,
    _parameters: new List<RequiredParameter>
    {
        new RequiredParameter(new IntParameterType(), "Number of repetitions"),
        new RequiredParameter(new StringParameterType(), "Message")
    },
    _description: "This command prints the message a specified number of times.",
    _runOtherCommandMethod: commandManager.ExecuteCommandAsync,
    _maxCommandDepth: 2
);
```

## Getting Started

### Example Usage
Here’s a basic example to demonstrate how to create and use commands:

```csharp
var commandManager = new CommandManager();

var helloCommand = new Command(
    _name: "hello",
    _action: HelloCommandAction,
    _parameters: new List<RequiredParameter>
    {
        new RequiredParameter(new IntParameterType(), "Times of saying hello"),
        new RequiredParameter(new StringParameterType(), "Your name")
    },
    _runOtherCommandMethod: commandManager.ExecuteCommandAsync,
    _maxCommandDepth: 2
);
commandManager.AddCommand(helloCommand);

// Command action function
public static string HelloCommandAction(ParametersData parametersData)
{
    string result = "";
    
    for(int i = 0; i < (int)parametersData.parametersValues[0].GetValue(); i++)
    {
        result += "Hello ";
    }

    result += (string)parametersData.parametersValues[1].GetValue();

    return result;
} 

// Example command execution
Console.WriteLine(commandManager.ExecuteCommandAsync("hello 5 \"peacemkr_png\"").Result);
// Output: Hello Hello Hello Hello Hello peacemkr_png
```

### Creating Custom Parameter Types
You can create and register your own parameter types:
```csharp
public class DateTimeParameterType : IParameterType
{
    public string TypeName => "DateTime";

    public Parameter Parse(string value)
    {
        if (DateTime.TryParse(value, out DateTime result))
        {
            return new DateTimeParameter(result);
        }
        else
        {
            throw new ParameterException($"Invalid DateTime value: {value}");
        }
    }
}

public class DateTimeParameter : Parameter
{
    private DateTime value;

    public DateTimeParameter(DateTime value)
    {
        this.value = value;
        this.ParameterType = new DateTimeParameterType();
    }

    public override object GetValue()
    {
        return value;
    }
}

// Register the new parameter type
ParameterTypeRegistry.RegisterParameterType(new DateTimeParameterType());
```
### Nested Commands
Commands can also be nested. Here’s an example:

```csharp
var lifeCommand = new Command(
    _name: "day-of-week",
    _action: parametersData => $"{((DateTime)parametersData.parametersValues[0].GetValue()).DayOfWeek}",
    _parameters: new List<RequiredParameter>
    {
        new RequiredParameter(new DateTimeParameterType(), "param1"),
    },
    _runOtherCommandMethod: commandManager.ExecuteCommandAsync,
    _maxCommandDepth: 2
);
commandManager.AddCommand(lifeCommand);
// Example of default using of command
Console.WriteLine(commandManager.ExecuteCommandAsync("day-of-week 21.09.2024").Result); 
//Output: Saturday

// Example of using a nested command
Console.WriteLine(commandManager.ExecuteCommandAsync("hello 2 {day-of-week 21.09.2024}").Result);
// Output: Hello Hello Saturday
```
### Command Parser Logic
The command parser handles various scenarios, such as:

* Quotation Marks: Used to define string parameters. E.g., "Your name".
* Curly Braces: Used for nested commands. E.g., {day-of-week 21.09.2024}.
* Parameter Separation: Parameters are separated by spaces, while nested commands are processed recursively.
### Error Handling
The system provides clear error messages for common issues, such as:

* Parameter type mismatches
* Missing required parameters
* Command nesting limit exceeded
### Contributing
Feel free to fork the repository and submit pull requests. Any contributions are welcome!

### License
This project is licensed under the MIT License. See the LICENSE file for details.

