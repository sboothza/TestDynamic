using System.Reflection;
using System.Runtime.Loader;

namespace DynamicLib;

public class DynamicLoadContext : AssemblyLoadContext, IDisposable
{
    private bool _disposed;

    private readonly AssemblyDependencyResolver _resolver;
    private readonly List<Assembly> _assemblies = new();
    private readonly string[] _overrideNames;

    public DynamicLoadContext(string pluginPath, params string[] overrideNames) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _overrideNames = overrideNames;
    }

    public void RegisterAssembly(Assembly assembly)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DynamicLoadContext));

        if (_assemblies.All(a => a.GetName().Name != assembly.GetName().Name))
            _assemblies.Add(assembly);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (!_overrideNames.Contains(assemblyName.Name))
        {
            var asm = _assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            if (asm != null)
                return asm;
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _assemblies.Clear();
                Unload();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DynamicLoadContext()
    {
        Dispose(false);
    }
}