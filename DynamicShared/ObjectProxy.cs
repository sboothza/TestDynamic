using System.Reflection;

namespace DynamicShared;

public class ObjectProxy
{
    private readonly object _root;
    private readonly Type _type;
    
    public ObjectProxy(object root)
    {
        _root = root;
        _type = root.GetType();
    }

    protected T CallMethod<T>(string name, params object[] parameters)
    {
        try
        {
            return (T)Convert.ChangeType(_type.GetMethod(name).Invoke(_root, parameters), typeof(T));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return default;
        }
    }

    protected T GetProperty<T>(string name)
    {
        var property = _type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property != null && property.CanRead)
            return (T)Convert.ChangeType(property.GetValue(_root), typeof(T));
        return default;
    }

    protected void SetProperty<T>(string name, T value)
    {
        var property = _type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property != null && property.CanWrite)
            property.SetValue(_root, value);
    }
}