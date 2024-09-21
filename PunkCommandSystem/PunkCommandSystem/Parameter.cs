using System.Collections.Generic;

namespace PunkCommandSystem
{
    public interface IParameterType
    {
        string TypeName { get; }
        Parameter Parse(string value);
    }

    public static class ParameterTypeRegistry
    {
        // Словарь для хранения всех доступных типов параметров
        private static readonly Dictionary<string, IParameterType> RegisteredTypes = new Dictionary<string, IParameterType>();

        // Метод для регистрации нового типа параметра
        public static void RegisterParameterType(IParameterType parameterType)
        {
            if (!RegisteredTypes.ContainsKey(parameterType.TypeName))
            {
                RegisteredTypes.Add(parameterType.TypeName, parameterType);
            }
        }

        // Метод для парсинга значения по типу
        public static Parameter Parse(string typeName, string value)
        {
            if (RegisteredTypes.TryGetValue(typeName, out var parameterType))
            {
                return parameterType.Parse(value);
            }
            else
            {
                throw new ParameterException($"Unknown parameter type: {typeName}");
            }
        }
    }

    public class IntParameterType : IParameterType
    {
        public string TypeName => "int";

        public Parameter Parse(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return new IntParameter(result);
            }
            else
            {
                throw new ParameterException($"Invalid int value: {value}");
            }
        }
    }

    public class FloatParameterType : IParameterType
    {
        public string TypeName => "float";

        public Parameter Parse(string value)
        {
            if (float.TryParse(value, out float result))
            {
                return new FloatParameter(result);
            }
            else
            {
                throw new ParameterException($"Invalid float value: {value}");
            }
        }
    }

    public class StringParameterType : IParameterType
    {
        public string TypeName => "string";

        public Parameter Parse(string value)
        {
             return new StringParameter(value);
        }
    }

    public class RequiredParameter
    {
        public IParameterType ParameterType { get; protected set; }
        public string name { get; protected set; }
        public string description { get; protected set; }
        public bool optional { get; protected set; }

        public RequiredParameter(IParameterType _type, string _name, string _description = "", bool _isOptional = false)
        {
            ParameterType = _type;
            this.name = _name;
            this.description = _description;
            this.optional = _isOptional;
        }

        public override string ToString()
        {
            return $"{ParameterType} - {name}";
        }
    }

    public abstract class Parameter
    {
        public IParameterType ParameterType { get; protected set; }

        // Абстрактный метод для получения значения, который будет переопределен в дочерних классах
        public abstract object GetValue();

        public override string ToString()
        {
            return $"{ParameterType} - {GetValue()}";
        }
    }

    public class IntParameter : Parameter
    {
        private int value;

        public IntParameter(int value)
        {
            this.value = value;
            this.ParameterType = new IntParameterType();
        }

        public override object GetValue()
        {
            return value;
        }
    }

    public class FloatParameter : Parameter
    {
        private float value;

        public FloatParameter(float value)
        {
            this.value = value;
            this.ParameterType = new FloatParameterType();
        }

        public override object GetValue()
        {
            return value;
        }
    }

    public class StringParameter : Parameter
    {
        private string value;

        public StringParameter(string value)
        {
            this.value = value;
            this.ParameterType = new StringParameterType();
        }

        public override object GetValue()
        {
            return value;
        }
    }
}
