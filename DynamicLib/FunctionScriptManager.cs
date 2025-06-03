namespace DynamicLib;

public class FunctionScriptManager() : ScriptManager(header: HEADER, footer: FOOTER, defaultTypename: "Script.Functions", assemblyName: "ScriptAssembly")
{
    private static string HEADER => @"using System;
using System.Collections.Generic;

namespace Script
{
    public static class Functions
    { 
        public static Dictionary<string, object> Context = new();";

    private static string FOOTER => " }}";
}