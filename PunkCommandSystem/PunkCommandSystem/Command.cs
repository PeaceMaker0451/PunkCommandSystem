using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PunkCommands
{
    public class ParameterException : Exception
    {
        public ParameterException(string message)
            : base(message) { }
    }

    public class ParametersData
    {
        public readonly List<Parameter> parametersValues;
        public readonly List<string> stringParametersValues;
        public readonly string rawParametersLine;

        public ParametersData(List<Parameter> _parametersValues, List<string> _stringParametersValues, string _rawParametersLine)
        {
            parametersValues = _parametersValues;
            stringParametersValues = _stringParametersValues;
            rawParametersLine = _rawParametersLine;
        }
    }


    public class Command
    {
        public delegate string CommandAction(ParametersData _parameters);
        public delegate Task<string> RunOtherCommandMethod(string _command);

        private readonly string name;
        private readonly string description;

        private readonly int maxCommandDepth;

        private readonly CommandAction action;

        private readonly RunOtherCommandMethod runOtherCommandMethod;
        private bool canExecuteOtherCommand;

        private readonly List<RequiredParameter> parameters;
        private List<Parameter> parametersValues;
        private List<string> stringParametersValues;
        private string rawParametersLine;

        public readonly int nonOptionalParametersCount;
        public readonly int optionalParametersCount;

        private const string WRONG_PARAMETER_ERROR = "Parameter type mismatch";
        private const string MISSING_PARAMETER_ERROR = "Missing required parameter";
        private const string EMPTY_COMMAND_ERROR = "You tried to execute command with empty string";
        private const string WRONG_COMMAND_ERROR = "You tried to execute command with mismatch name";
        private const string NULL_NAME_ERROR = "Command name is null";
        private const string NULL_ACTION_ERROR = "Command action is null";
        private const string OPTIONAL_PARAMETRS_NOT_LAST_ERROR = "Optional parameters must be after others";
        private const string COMMAND_NESTING_LIMIT_EXCEEDED_ERROR = "Command nesting level exceeded the allowed limit";


        static Command()
        {
            ParameterTypeRegistry.RegisterParameterType(new IntParameterType());
            ParameterTypeRegistry.RegisterParameterType(new FloatParameterType());
            ParameterTypeRegistry.RegisterParameterType(new StringParameterType());
        }
        
        public Command(string _name, CommandAction _action, List<RequiredParameter> _parameters, string _description = "", RunOtherCommandMethod _runOtherCommandMethod = null, int _maxCommandDepth = 3) 
        {
            if (_name == null)
                throw new ArgumentNullException(NULL_NAME_ERROR);

            if (_action == null)
                throw new ArgumentNullException(NULL_ACTION_ERROR);

            if (_parameters == null)
                _parameters = new List<RequiredParameter>();
            
            name = _name;
            action = _action;
            parameters = _parameters;
            description = _description;
            maxCommandDepth = _maxCommandDepth;

            if (_runOtherCommandMethod != null)
            {
                canExecuteOtherCommand = true;
                runOtherCommandMethod = _runOtherCommandMethod;
            }
            else
            {
                canExecuteOtherCommand = false;
            }

            int _nonOptionalParameters = 0;
            int _optionalParameters = 0;
            bool _lastIsOptional = false;

            foreach(RequiredParameter parameter in parameters)
            {
                if(!parameter.optional)
                {
                    if(_lastIsOptional)
                        throw new InvalidOperationException(OPTIONAL_PARAMETRS_NOT_LAST_ERROR);

                    _nonOptionalParameters++;
                    _lastIsOptional = false;
                }
                else
                {
                    _optionalParameters++;
                    _lastIsOptional = true;
                }
            }

            optionalParametersCount = _optionalParameters;
            nonOptionalParametersCount = _nonOptionalParameters;
        }

        public async Task<string> RunCommand(string _command)
        {
            try
            {
                await ReadParameters(_command);
            }
            catch (ParameterException e)
            {
                return ParameterExceptionAction(e);
            }
            
            return ExecuteAction();
        }

        public string Name()
        {
            return name;
        }
        public string Description()
        {
            if (description != null)
                return description;
            else
                return "";
        }

        public List<RequiredParameter> RequiredParameters()
        {
            return parameters;
        }

        public virtual bool CanBeExecuted(string _command)
        {
            return EqualsName(_command);
        }

        private Task<bool> ReadParameters(string _command)
        {
            if (_command == null || _command == "" || _command == " ")
                throw new ArgumentException(EMPTY_COMMAND_ERROR);

            if (!EqualsName(_command))
                throw new ArgumentException(WRONG_COMMAND_ERROR);

            rawParametersLine = _command.Remove(0, name.Length);
            stringParametersValues = SplitParametersLine(rawParametersLine);

            /*if(stringParametersValues.Count < nonOptionalParametersCount)
                throw new ParameterException(MISSING_PARAMETER_ERROR);*/

            parametersValues = new List<Parameter>();

            for (int i = 0; i < parameters.Count; i++)
            {
                var requiredParameter = parameters[i];
                var parameterValue = i < stringParametersValues.Count ? stringParametersValues[i] : null;

                if (parameterValue == null && !requiredParameter.optional)
                    throw new ParameterException($"{MISSING_PARAMETER_ERROR}: {requiredParameter.name}");

                if (parameterValue != null)
                {
                    try
                    {
                        // Используем статический класс для парсинга значения
                        var parsedParameter = ParameterTypeRegistry.Parse(requiredParameter.ParameterType.TypeName, parameterValue);
                        parametersValues.Add(parsedParameter);
                    }
                    catch (Exception ex)
                    {
                        throw new ParameterException($"{WRONG_PARAMETER_ERROR} {requiredParameter.name}: {ex.Message}");
                    }
                }
            }


            return Task.FromResult(true);
        }

        private List<string> SplitParametersLine(string parametersLine)
        {
            bool isParsingStarted = false;  // Флаг начала разбора строки
            bool isInsideString = false;    // Флаг того, что мы находимся внутри строки в кавычках
            bool isInsideCommand = false;   // Флаг того, что мы находимся внутри команды в фигурных скобках
            bool isLastCharEscaped = false; // Флаг того, что предыдущий символ был командным или строковым завершителем

            List<string> commandParts = new List<string>();  // Список для хранения вложенных команд
            int commandDepth = -1;  // Счетчик вложенных команд
            List<string> parameters = new List<string>();  // Итоговый список параметров
            StringBuilder currentToken = new StringBuilder();  // Текущий собираемый токен (подстрока)

            // Добавляем пробел в конце для удобства завершения последнего параметра
            parametersLine += " ";

            foreach (char currentChar in parametersLine)
            {
                // Пропускаем ведущие пробелы до начала обработки параметров
                if (!isParsingStarted && currentChar == ' ')
                {
                    continue;
                }

                isParsingStarted = true;

                // Обработка начала команды { ... }
                if (currentChar == '{' && !isInsideString && !isLastCharEscaped)
                {
                    if (maxCommandDepth != 0 && commandDepth + 1 >= maxCommandDepth)
                    {
                        throw new InvalidOperationException(COMMAND_NESTING_LIMIT_EXCEEDED_ERROR);
                    }

                    isInsideCommand = true;
                    commandParts.Add("");  // Начинаем новую команду
                    commandDepth++;
                    isLastCharEscaped = false;
                    continue;
                }

                // Обработка завершения команды }
                if (currentChar == '}' && commandDepth >= 0)
                {
                    string executedCommand = TryExecuteOtherCommand("{" + commandParts[commandDepth] + "}");
                    if (commandDepth == 0)
                    {
                        // Закрыли основную команду — добавляем результат как параметр
                        parameters.Add(executedCommand);
                    }
                    else
                    {
                        // Закрыли вложенную команду — добавляем результат к предыдущей команде
                        commandParts[commandDepth - 1] += executedCommand;
                    }

                    commandParts.RemoveAt(commandDepth);
                    commandDepth--;
                    isInsideCommand = commandDepth >= 0;  // Обновляем состояние, внутри ли мы еще команды
                    isLastCharEscaped = true;
                    continue;
                }

                // Обработка кавычек для строк "...".
                if (currentChar == '\"' && !isInsideCommand)
                {
                    if (isInsideString)
                    {
                        // Завершение строки
                        parameters.Add(currentToken.ToString());
                        currentToken.Clear();
                        isInsideString = false;
                        isLastCharEscaped = true;
                    }
                    else
                    {
                        // Начало строки
                        isInsideString = true;
                        isLastCharEscaped = false;
                    }
                    continue;
                }

                // Обработка пробелов
                if (currentChar == ' ' && !isInsideString && !isInsideCommand)
                {
                    if (currentToken.Length > 0)
                    {
                        // Завершаем сбор текущего параметра и добавляем его в список
                        parameters.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    isLastCharEscaped = false;
                    continue;
                }

                // Добавляем символы в команду или параметр в зависимости от контекста
                if (isInsideCommand)
                {
                    commandParts[commandDepth] += currentChar;
                }
                else
                {
                    currentToken.Append(currentChar);
                }

                // Сброс флага "последний символ был завершителем" для следующих символов
                isLastCharEscaped = false;
            }

            return parameters;
        }

        protected virtual string ParameterExceptionAction(ParameterException e)
        {
            throw e;
        }

        protected virtual string ExecuteAction()
        {
            return action(new ParametersData(parametersValues,stringParametersValues,rawParametersLine));
        }

        private string TryExecuteOtherCommand(string command)
        {
            if(command.StartsWith("{") && command.EndsWith("}"))
            {
                command = command.Trim('{', '}');
            }
            else
            {
                return command;
            }
            
            if(canExecuteOtherCommand)
            {
                
                try
                {
                    return runOtherCommandMethod(command).Result;
                }
                catch (KeyNotFoundException e)
                {
                    return "{" + command + "}";
                }
                catch (Exception e)
                {
                    throw e;
                }
                
            }

            return "{" + command + "}";
        }

        private bool EqualsName(string _command)
        {
            string[] subStrings = _command.Split(new char[] { ' ' });

            if (subStrings[0] != name)
                return false;
            else
                return true;
        }

        public Command Clone()
        {
            // Создание новой команды с теми же параметрами и действиями
            return new Command(
                _name: this.name,
                _action: this.action,
                _parameters: new List<RequiredParameter>(this.parameters),
                _description: this.description,
                _runOtherCommandMethod: this.runOtherCommandMethod,
                _maxCommandDepth: this.maxCommandDepth
            );
        }
    }
}
