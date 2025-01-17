using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWReactiveUI;

internal static class Descriptors
{
    public static readonly DiagnosticDescriptor NotPartialClass =
        new(
            id: "R0001",
            title: "Not partial class",
            messageFormat: "This class implemented IReactiveObject but it is not partial class, place add partial key word",
            category: "HKWReactiveUI",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor PropertyHasSetMethod =
        new(
            id: "R0002",
            title: "Property has SetMethod",
            messageFormat: "Feature {0} is not valid for property with SetMethod",
            category: "HKWReactiveUI",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    //public static readonly DiagnosticDescriptor ConverterDescriptor =
    //    new(
    //        id: "R0003",
    //        title: "Converter not implemented IConverter interface",
    //        messageFormat: "Converter \"{0}\", not implemented IMapConverter interface",
    //        category: "HKWReactiveUI",
    //        DiagnosticSeverity.Error,
    //        isEnabledByDefault: true
    //    );
}
