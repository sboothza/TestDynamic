using System.Reflection;

namespace DynamicLib;

public class DynamicAssemblyManager : IDisposable
{
    private bool _disposed;
    DynamicLoadContext _context;
    private ScriptManager _scriptManager;

    public DynamicAssemblyManager(string assemblyName, params string[] overrideNames)
    {
        _context = new DynamicLoadContext(Assembly.GetExecutingAssembly().Location, overrideNames);
        _context.Unloading += (ctx) => { _scriptManager?.Dispose(); };
        _scriptManager = new ScriptManager(defaultTypename: "", assemblyName: assemblyName, loadContext: _context);
    }

    public void AddScriptFromFile(string script)
    {
        _scriptManager.AddScriptFromFile(script);
    }

    public bool Build()
    {
        var result = _scriptManager.Build();
        if (result.Item1)
            _context.RegisterAssembly(_scriptManager.Assembly);

        return result.Item1;
    }

    public Assembly LoadAssembly(string name)
    {
        return _context.LoadFromAssemblyName(new AssemblyName(name));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _scriptManager.Dispose();
                _context.Unload();
                _context.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DynamicAssemblyManager()
    {
        Dispose(false);
    }
}