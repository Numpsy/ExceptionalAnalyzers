using Exceptional.Analyzers.Analyzers;
using Exceptional.Analyzers.Fixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Exceptional.Analyzers.Test
{
    [TestClass]
    public class ThrowSiteAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        public void When_no_exception_is_thrown_then_everything_is_fine()
        {
            var test = @"";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void When_exception_is_thrown_in_method_and_not_documented_then_diagnostic_is_available()
        {
            //// Act
            var test = @"
    using System;

    namespace MyNamespace
    {
        class MyClass
        {   
            public void MyMethod() 
            {
                throw new Exception();
            }
        }
    }";
            var fixtest = @"
    using System;

    namespace MyNamespace
    {
        class MyClass
        {   
            /// <exception cref=""Exception"">An exception occured.</exception>
            public void MyMethod() 
            {
                throw new Exception();
            }
        }
    }";

            VerifyCSharpFix(test, fixtest);
            //// Assert
            var expected = new DiagnosticResult
            {
                Id = ThrowSiteAnalyzer.DiagnosticId,
                Message = string.Format(Resources.AnalyzerMessageFormat, "System.Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };

            VerifyCSharpDiagnostic(test, expected);
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void When_exception_is_thrown_in_method_and_documented_then_no_diagnostic()
        {
            //// Act
            var test = @"
    using System;

    namespace MyNamespace
    {
        class MyClass
        {   
            /// <exception cref=""Exception"">An exception occured.</exception>
            public void MyMethod() 
            {
                throw new Exception();
            }
        }
    }";

            //// Assert
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void When_exception_is_thrown_in_property_getter_and_not_documented_then_diagnostic_is_available()
        {
            //// Act
            var test = @"
    using System;

    namespace MyNamespace
    {
        class MyClass
        {   
            public string MyProperty
            {
                get 
                {
                    throw new Exception();
                }
            }
        }
    }";

            //// Assert
            var expected = new DiagnosticResult
            {
                Id = ThrowSiteAnalyzer.DiagnosticId,
                Message = string.Format(Resources.AnalyzerMessageFormat, "System.Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void When_exception_is_thrown_and_catched_then_everything_is_fine()
        {
            //// Act
            var test = @"
    using System;

    namespace MyNamespace
    {
        class MyClass
        {   
            public void MyMethod() 
            {
                try
                {
                    throw new Exception();
                }
                catch 
                {

                }
            }
        }
    }";

            //// Assert
            VerifyCSharpDiagnostic(test);
        }


        //Diagnostic and CodeFix both triggered and checked for
        //[TestMethod]
        public void TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            /// <summary>Test.</summary>
            /// <exception cref=""System.Exception"">An exception.</exception>
            public void Test() 
            {
                throw new Exception();
            }
        }
    }";

            var expected = new DiagnosticResult
            {
                Id = "ExceptionalAnalyzers",
                Message = $"Type name '{"TypeName"}' contains lowercase letters",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 15) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ExceptionalAnalyzersCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ThrowSiteAnalyzer();
        }
    }
}