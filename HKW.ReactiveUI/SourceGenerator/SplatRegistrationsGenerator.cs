//using System;
//using System.Collections.Generic;
//using System.Text;
//using HKW.SourceGeneratorUtils;

//namespace HKW.HKWReactiveUI.SourceGenerator;

//internal class SplatRegistrationsGenerator
//{
//    public static void Generate(ClassInfo classInfo)
//    {
//        var g = new SplatRegistrationsGenerator(classInfo);
//        g.Process();
//    }

//    private readonly ClassInfo _classInfo;

//    public SplatRegistrationsGenerator(ClassInfo classInfo)
//    {
//        _classInfo = classInfo;
//    }

//    private void Process()
//    {
//        for (var i = 0; i < _classInfo.MethodSymbols.Count; i++)
//        {
//            var methodSymbol = _classInfo.MethodSymbols[i];
//            ProcessMethod(methodSymbol);
//        }
//    }

//    private void ProcessMethod(MethodSS methodSS)
//    {
//        methodSS.OutData(out var methodSyntax, out var methodSymbol);
//    }
//}
