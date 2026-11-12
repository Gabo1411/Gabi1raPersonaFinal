using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

// Registro de comandos: busca por reflexión los métodos marcados con [ConsoleCommand], separa la línea escrita
// en palabras (con comillas) y ofrece las opciones del autocompletado.
public static class CommandRegistry
{
    public class Command
    {
        public ConsoleCommandAttribute info;
        public MethodInfo method;
        public Dictionary<string, string[]> options = new Dictionary<string, string[]>();   // primer argumento -> opciones del segundo
        public string[] firstOptions = new string[0];
    }

    static Dictionary<string, Command> commands;

    public static IEnumerable<Command> All
    {
        get
        {
            Build();
            List<Command> list = new List<Command>(commands.Values);
            list.Sort((a, b) => string.Compare(a.info.Name, b.info.Name, StringComparison.Ordinal));
            return list;
        }
    }

    static void Build()
    {
        if (commands != null) return;
        commands = new Dictionary<string, Command>();

        foreach (MethodInfo method in typeof(DebugCommands).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            ConsoleCommandAttribute attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
            if (attribute == null) continue;

            Command command = new Command { info = attribute, method = method };
            if (!string.IsNullOrEmpty(attribute.Options))
            {
                List<string> first = new List<string>();
                foreach (string token in attribute.Options.Split(','))
                {
                    string[] parts = token.Split(':');
                    first.Add(parts[0]);
                    if (parts.Length > 1) command.options[parts[0]] = parts[1].Split('/');
                }
                command.firstOptions = first.ToArray();
            }
            commands[attribute.Name.ToLowerInvariant()] = command;
        }
    }

    public static bool TryGet(string name, out Command command)
    {
        Build();
        return commands.TryGetValue(name.ToLowerInvariant(), out command);
    }

    // Ejecuta una línea completa y devuelve el texto a mostrar (isError = true para pintarlo en rojo)
    public static string Execute(string line, out bool isError)
    {
        isError = false;
        List<string> words = Split(line);
        if (words.Count == 0) return null;

        if (!TryGet(words[0], out Command command))
        {
            isError = true;
            return "Comando desconocido: '" + words[0] + "'. Escribe help para ver la lista.";
        }

        string[] args = words.GetRange(1, words.Count - 1).ToArray();
        try
        {
            object result = command.method.Invoke(null, new object[] { args });
            return result as string ?? "ok";
        }
        catch (TargetInvocationException e)
        {
            isError = true;
            Exception inner = e.InnerException ?? e;
            return inner is CommandException ? inner.Message : "Error: " + inner.GetType().Name + ": " + inner.Message;
        }
        catch (Exception e)
        {
            isError = true;
            return "Error: " + e.Message;
        }
    }

    // Separa por espacios respetando las comillas
    public static List<string> Split(string line)
    {
        List<string> words = new List<string>();
        StringBuilder current = new StringBuilder();
        bool quoted = false;
        foreach (char c in line)
        {
            if (c == '"') { quoted = !quoted; continue; }
            if (char.IsWhiteSpace(c) && !quoted)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Length = 0; }
            }
            else current.Append(c);
        }
        if (current.Length > 0) words.Add(current.ToString());
        return words;
    }

    // Candidatos de autocompletado para la línea actual. 'prefix' devuelve la parte ya escrita de la última palabra.
    public static List<string> Complete(string line, out string prefix)
    {
        Build();
        List<string> result = new List<string>();
        List<string> words = Split(line);
        bool endsWithSpace = line.Length > 0 && char.IsWhiteSpace(line[line.Length - 1]);
        if (endsWithSpace) words.Add("");
        prefix = words.Count > 0 ? words[words.Count - 1] : "";

        IEnumerable<string> pool;
        if (words.Count <= 1)
        {
            List<string> names = new List<string>();
            foreach (Command c in All) names.Add(c.info.Name);
            pool = names;
        }
        else if (TryGet(words[0], out Command command))
        {
            if (words.Count == 2) pool = command.firstOptions;
            else if (words.Count == 3 && command.options.TryGetValue(words[1].ToLowerInvariant(), out string[] second)) pool = second;
            else pool = new string[0];
        }
        else pool = new string[0];

        foreach (string candidate in pool)
        {
            if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) result.Add(candidate);
        }
        return result;
    }

    // ---------- Lectura de argumentos ----------

    public static int Int(string[] args, int index, int fallback)
    {
        if (args.Length <= index) return fallback;
        if (int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) return value;
        throw new CommandException("'" + args[index] + "' no es un número entero.");
    }

    public static float Float(string[] args, int index, float fallback)
    {
        if (args.Length <= index) return fallback;
        if (float.TryParse(args[index].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return value;
        throw new CommandException("'" + args[index] + "' no es un número.");
    }

    // on/off/1/0/true/false; sin argumento devuelve 'toggleFrom' invertido
    public static bool Switch(string[] args, int index, bool current)
    {
        if (args.Length <= index) return !current;
        switch (args[index].ToLowerInvariant())
        {
            case "on": case "1": case "true": case "si": case "sí": return true;
            case "off": case "0": case "false": case "no": return false;
        }
        throw new CommandException("Usa on u off.");
    }
}

// Error esperado de un comando: se muestra tal cual, sin el nombre de la excepción
public class CommandException : Exception
{
    public CommandException(string message) : base(message) { }
}
