using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PunkCommandSystem
{
    public class CommandManager
    {
        private readonly Dictionary<string, Command> _commands;

        public CommandManager()
        {
            _commands = new Dictionary<string, Command>();
        }

        // Добавить команду в менеджер
        public void AddCommand(Command command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (_commands.ContainsKey(command.Name()))
                throw new InvalidOperationException($"Command with name '{command.Name()}' already exists.");

            _commands.Add(command.Name(), command);
        }

        // Удалить команду из менеджера
        public void RemoveCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
                throw new ArgumentNullException(nameof(commandName));

            if (!_commands.ContainsKey(commandName))
                throw new KeyNotFoundException($"Command with name '{commandName}' not found.");

            _commands.Remove(commandName);
        }

        // Запустить команду
        public async Task<string> ExecuteCommandAsync(string commandString)
        {
            if (string.IsNullOrEmpty(commandString))
                throw new ArgumentNullException(nameof(commandString));

            var commandName = ExtractCommandName(commandString);

            if (!_commands.TryGetValue(commandName, out var command))
                throw new KeyNotFoundException($"Command with name '{commandName}' not found.");

            var commandCopy = command.Clone();

            return await commandCopy.RunCommand(commandString);
        }

        public string ExecuteCommand(string commandString)
        {
            if (string.IsNullOrEmpty(commandString))
                throw new ArgumentNullException(nameof(commandString));

            var commandName = ExtractCommandName(commandString);

            if (!_commands.TryGetValue(commandName, out var command))
                throw new KeyNotFoundException($"Command with name '{commandName}' not found.");

            var commandCopy = command.Clone();

            return commandCopy.RunCommand(commandString).Result;
        }

        // Извлечь имя команды из строки команды
        private static string ExtractCommandName(string commandString)
        {
            if (string.IsNullOrEmpty(commandString))
                throw new ArgumentException("Command string is null or empty.", nameof(commandString));

            var parts = commandString.Split(new[] { ' ' }, 2);
            return parts[0];
        }
    }
}
