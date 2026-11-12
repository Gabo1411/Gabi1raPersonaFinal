using System;

// Marca un método estático de DebugCommands como comando de la consola.
// El método recibe los argumentos ya separados y devuelve el texto de respuesta (o null para un "ok" genérico).
[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommandAttribute : Attribute
{
    public string Name;
    public string Help;
    public string Usage;

    // Opciones para el autocompletado (Tab). Separadas por comas; "opcion:a/b/c" añade las opciones del siguiente argumento.
    // Ejemplo: "hp,kill,attack:bullets/gatling/missiles,freeze"
    public string Options;

    public ConsoleCommandAttribute(string name, string help, string usage = null, string options = null)
    {
        Name = name;
        Help = help;
        Usage = usage ?? name;
        Options = options;
    }
}
