namespace DynamicShared;

public class TestObjProxy(object root) : ObjectProxy(root), ITestObj
{
    public void DoStuff(string name) => CallMethod<object>("DoStuff", name);

    public string Value1
    {
        get => GetProperty<string>("Value1");
        set => SetProperty("Value1", value);
    }

    public string Value2
    {
        get => GetProperty<string>("Value2");
        set => SetProperty("Value2", value);
    }
}