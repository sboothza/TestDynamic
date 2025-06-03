using System.Reflection;

namespace DynamicShared;

public class TestObjProxy : ITestObj
{
    private readonly object _root;
    private readonly Type _type;

    public TestObjProxy(object root)
    {
        _root = root;
        _type = root.GetType();
    }

    public void DoStuff(string name)
    {
        _type.GetMethod("DoStuff").Invoke(_root, [name]);
    }

    public string Value1
    {
        get
        {
            var property = _type.GetProperty("Value1", BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
                return (string)property.GetValue(_root);
            return string.Empty;
        }
        set
        {
            var property = _type.GetProperty("Value1", BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
                property.SetValue(_root, value);
        }
    }

    public string Value2
    {
        get
        {
            var property = _type.GetProperty("Value2", BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
                return (string)property.GetValue(_root);
            return string.Empty;
        }
        set
        {
            var property = _type.GetProperty("Value2", BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
                property.SetValue(_root, value);
        }
    }
}