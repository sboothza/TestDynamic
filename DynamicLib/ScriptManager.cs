using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DynamicLib;

public class ScriptManager : IDisposable
{
    private bool _disposed;

    protected readonly StringBuilder _scripts = new(1024);
    protected Assembly? _assembly;

    public Assembly? Assembly => _assembly;

    private readonly List<string> _references = [];

    protected readonly string _header;
    protected readonly string _footer;

    protected readonly string _defaultTypename;
    protected readonly string _assemblyName;

    protected readonly AssemblyLoadContext _loadContext;

    public ScriptManager(string header = "", string footer = "", string defaultTypename = "", string assemblyName = "",
        AssemblyLoadContext? loadContext = null)
    {
        _assembly = null;
        _header = header;
        _footer = footer;
        _defaultTypename = defaultTypename;
        _assemblyName = assemblyName;
        _loadContext = loadContext ?? throw new ArgumentNullException(nameof(loadContext),
            "A collectible AssemblyLoadContext must be provided");
    }

    public void AddScript(string script)
    {
        _scripts.AppendLine(script);
    }

    public void AddReference(string assemblyName)
    {
        var rootPath = Path.GetDirectoryName(typeof(object).Assembly.Location) + Path.DirectorySeparatorChar;
        _references.Add(Path.Combine(rootPath, assemblyName));
    }

    public void AddLocalReference(string assemblyName)
    {
        var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                       Path.DirectorySeparatorChar;
        _references.Add(Path.Combine(rootPath, assemblyName));
    }

    public void AddScriptFromFile(string code)
    {
        const string localRegex = "#Local:((.+),?)+";
        const string systemRegex = "#System:((.+),?)+";

        if (Regex.IsMatch(code, localRegex))
        {
            var localReferences = Regex.Match(code, localRegex).Groups[1].Value.Split(',');
            localReferences.ToList().ForEach(AddLocalReference);
            code = Regex.Replace(code, localRegex, string.Empty);
        }

        if (Regex.IsMatch(code, systemRegex))
        {
            var systemReferences = Regex.Match(code, systemRegex).Groups[1].Value.Split(',');
            systemReferences.ToList().ForEach(AddReference);
            code = Regex.Replace(code, systemRegex, string.Empty);
        }

        _scripts.AppendLine(code);
    }

    protected virtual CSharpCompilation AddReferences(CSharpCompilation compilation)
    {
        var comp = compilation;

        foreach (var reference in _references)
            comp = comp.AddReferences(MetadataReference.CreateFromFile(reference));

        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        comp = comp.AddReferences(mscorlib);
        var runtime =
            MetadataReference.CreateFromFile(Path.Join(Path.GetDirectoryName(typeof(object).Assembly.Location),
                "System.Runtime.dll"));
        comp = comp.AddReferences(runtime);

        return comp;
    }

    public virtual (bool, IEnumerable<Diagnostic>?) Build()
    {
        var tree = CSharpSyntaxTree.ParseText($"{_header}{_scripts}{_footer}");

        var compilation = CSharpCompilation.Create(_assemblyName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(tree);

        compilation = AddReferences(compilation);

        using (var ms = new MemoryStream())
        {
            var emitResult = compilation.Emit(ms, pdbStream: null);

            if (!emitResult.Success)
            {
                return (emitResult.Success, emitResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error));
            }

            ms.Seek(0, SeekOrigin.Begin);
            _assembly = _loadContext.LoadFromStream(ms);
        }

        return (true, null);
    }

    public T? GetPropertyOrField<T>(string propertyOrFieldName, string typeName = "")
    {
        if (string.IsNullOrEmpty(typeName))
            typeName = _defaultTypename;

        var t = _assembly?.GetType(typeName);
        if (t != null)
        {
            var f = t.GetField(propertyOrFieldName, BindingFlags.Static | BindingFlags.Public);
            if (f != null)
                return (T)f.GetValue(null);

            var p = t.GetProperty(propertyOrFieldName, BindingFlags.Static | BindingFlags.Public);
            if (p != null)
                return (T)p.GetValue(null);
        }

        return default;
    }

    public void SetPropertyOrField(string propertyOrFieldName, object value, string typeName = "")
    {
        if (string.IsNullOrEmpty(typeName))
            typeName = _defaultTypename;
        var t = _assembly?.GetType(typeName);
        if (t != null)
        {
            var f = t.GetField(propertyOrFieldName, BindingFlags.Static | BindingFlags.Public);
            if (f != null)
                f.SetValue(null, value);

            var p = t.GetProperty(propertyOrFieldName, BindingFlags.Static | BindingFlags.Public);
            if (p != null)
                p.SetValue(null, value);
        }
    }

    public object? ExecuteMethod(string methodName, string typeName = "", params object[] parameters)
    {
        if (string.IsNullOrEmpty(typeName))
            typeName = _defaultTypename;
        var t = _assembly?.GetType(typeName);
        if (t != null)
        {
            var method = t.GetMethod(methodName);
            if (method != null)
            {
                var result = method.Invoke(null, BindingFlags.Static, null, parameters, null);
                return result;
            }
        }

        return null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _references.Clear();
                _scripts.Clear();
                _assembly = null;

                if (_loadContext is IDisposable disposable)
                    disposable.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ScriptManager()
    {
        Dispose(false);
    }
}