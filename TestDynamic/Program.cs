using DynamicLib;
using DynamicShared;

namespace TestDynamic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("starting");

        // for (var i = 0; i < 1000; i++)
        // {
        //     Console.WriteLine(i);

        using (var dm = new DynamicAssemblyManager("TestAssembly", "DynamicShared"))
        {
            var script = File.ReadAllText("TestObj1.csscript");
            dm.AddScriptFromFile(script);
            var result = dm.Build();
            var asm = dm.LoadAssembly("TestAssembly");

            var rawObj = asm.CreateInstance("Test.TestObj1");
            ITestObj obj = null;
            try
            {
                obj = (ITestObj)asm.CreateInstance("Test.TestObj1"); //fails, but below....
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            var objType = rawObj.GetType();
            Console.WriteLine(objType.GetInterfaces()[0].FullName); //wtf????

            objType.GetMethod("DoStuff").Invoke(rawObj, ["Bob"]);

            var proxy = new TestObjProxy(rawObj)
            {
                Value1 = "Hello",
                Value2 = "World"
            };

            proxy.DoStuff("Bob");

            //small memory leak somewhere...
            objType = null;
            proxy = null;
            rawObj = null;
            obj = null;
            asm = null;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        //     Thread.Sleep(100);
        // }
    }
}