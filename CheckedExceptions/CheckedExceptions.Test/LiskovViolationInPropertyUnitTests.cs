using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CheckedExceptions.Test
{
    [TestClass]
    public class LiskovViolationInPropertyUnitTests : CodeFixVerifier
    {
        /// <summary>
        /// TestGetterWithExceptionNotDeclaredInBase
        /// </summary>
        [TestMethod]
        public void TestGetterWithExceptionNotDeclaredInBase()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    public class HelloWorldBase
    {
        /// <summary>
        /// Foo
        /// </summary>
        public int Foo { get; }
    }
        
    public class HelloWorld : HelloWorldBase
    {   
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Get. Some text</exception>
        public int Foo
        {
            get
            {
                throw new FieldAccessException();
            }
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = LiskovViolationsPropertyAnalyzer.BasePropertyGetterViolationsDiagnosticId,
                Message = string.Format(LiskovViolationsPropertyAnalyzer.BasePropertyGetterViolationsMessageFormat, "Foo", "FieldAccessException", "HelloWorldBase"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 20)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
using System;

namespace ConsoleApplication1
{
    public class HelloWorldBase
    {
        /// <summary>
        /// Foo
        /// </summary>
        public int Foo { get; }
    }
        
    public class HelloWorld : HelloWorldBase
    {   
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Get. Ignore. Some text</exception>
        public int Foo
        {
            get
            {
                throw new FieldAccessException();
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestGetterInterfaceWithExceptionNoSetter
        /// </summary>
        [TestMethod]
        public void TestGetterInterfaceWithExceptionNoSetter()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    public interface IHelloWorld
    {
        int Foo { get; }
    }
        
    public class HelloWorld : IHelloWorld
    {   
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Get. Ignore. Some text</exception>
        /// <exception cref=""Exception"">Setter. Some text</exception>
        public int Foo
        {
            get
            {
                throw new FieldAccessException();
            }
            set
            {
                throw new Exception();
            }
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestGetterBaseWithExceptionNoSetter
        /// </summary>
        [TestMethod]
        public void TestGetterBaseWithExceptionNoSetter()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    public class HelloWorldBase
    {
        public int Foo { get; }
    }
        
    public class HelloWorld : HelloWorldBase
    {   
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Get. Ignore. Some text</exception>
        /// <exception cref=""Exception"">Set. Some text</exception>
        public int Foo
        {
            get
            {
                throw new FieldAccessException();
            }
            set
            {
                throw new Exception();
            }
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestGetterNotDeclaredInInterface
        /// </summary>
        [TestMethod]
        public void TestGetterNotDeclaredInInterface()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    public interface IHelloWorld
    {
        /// <summary>
        /// Foo
        /// </summary>
        int Foo { get; }
    }
        
    public class HelloWorld : IHelloWorld
    {   
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Get.</exception>
        public int Foo
        {
            get
            {
                throw new FieldAccessException();
            }
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = LiskovViolationsPropertyAnalyzer.BasePropertyGetterViolationsDiagnosticId,
                Message = string.Format(LiskovViolationsPropertyAnalyzer.BasePropertyGetterViolationsMessageFormat, "Foo", "FieldAccessException", "IHelloWorld"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 20)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
using System;

namespace ConsoleApplication1
{
    public interface IHelloWorld
    {
        /// <summary>
        /// Foo
        /// </summary>
        int Foo { get; }
    }
        
    public class HelloWorld : IHelloWorld
    {   
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Get. Ignore.</exception>
        public int Foo
        {
            get
            {
                throw new FieldAccessException();
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new LiskovViolationsCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LiskovViolationsPropertyAnalyzer();
        }
    }
}