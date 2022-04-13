using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CheckedExceptions.Test
{
    [TestClass]
    public class LiskovViolationInMethodUnitTests : CodeFixVerifier
    {
        /// <summary>
        /// TestMethodWithExceptionNotDeclaredInInterface
        /// </summary>
        [TestMethod]
        public void TestMethodWithExceptionNotDeclaredInInterface()
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
        interface IHelloWorld
        {
            int Foo (int i);
        }

        class HelloWorld : IHelloWorld
        {
            /// <summary>
            /// Foo
            /// </summary>
            /// <param name=""i""></param>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException""></exception>
            public int Foo(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = LiskovViolationsMemberAnalyzer.InterfaceViolationsDiagnosticId,
                Message = String.Format(LiskovViolationsMemberAnalyzer.InterfaceViolationsMessageFormat, "Foo", "FieldAccessException", "IHelloWorld"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 24, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        interface IHelloWorld
        {
            int Foo (int i);
        }

        class HelloWorld : IHelloWorld
        {
            /// <summary>
            /// Foo
            /// </summary>
            /// <param name=""i""></param>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException"">Ignore.</exception>
            public int Foo(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestMethodWithExceptionNotDeclaredInBase
        /// </summary>
        [TestMethod]
        public void TestMethodWithExceptionNotDeclaredInBase()
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
        public class HelloWorldBase
        {
            /// <summary>
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <param name=""i""></param>
            public int Foo(int i)
            {
            }
        }
        
        public class HelloWorld : HelloWorldBase
        {   
            /// <summary>
            /// Foo
            /// </summary>
            /// <param name=""i""></param>
            /// <exception cref=""FieldAccessException"">Some text</exception>
            public int Foo(int i)
            {
                throw new FieldAccessException;
            }
        }
    }";

            var expected = new DiagnosticResult
            {
                Id = LiskovViolationsMemberAnalyzer.BaseMethodViolationsDiagnosticId,
                Message = string.Format(LiskovViolationsMemberAnalyzer.BaseMethodViolationsMessageFormat, "Foo", "FieldAccessException", "HelloWorldBase"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                new[] {
                            new DiagnosticResultLocation("Test0.cs", 30, 24)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        public class HelloWorldBase
        {
            /// <summary>
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <param name=""i""></param>
            public int Foo(int i)
            {
            }
        }
        
        public class HelloWorld : HelloWorldBase
        {   
            /// <summary>
            /// Foo
            /// </summary>
            /// <param name=""i""></param>
            /// <exception cref=""FieldAccessException"">Ignore. Some text</exception>
            public int Foo(int i)
            {
                throw new FieldAccessException;
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestIgnore
        /// </summary>
        [TestMethod]
        public void TestIgnore()
        {
            var test = @"
using System;

namespace ConsoleApplication1 
{
    interface IAbstractValidateur 
    {
        void ValidateAndContinueWith();
    }

    class AbstractValidateur : IAbstractValidateur 
    {
        /// <summary>
        /// ValidateAndContinueWith
        /// </summary>
        /// <exception cref=""System.Reflection.TargetInvocationException""></exception>
        /// <exception cref=""MethodAccessException""></exception>
        /// <exception cref=""MemberAccessException""></exception>
        /// <exception cref=""System.Runtime.InteropServices.InvalidComObjectException""></exception>
        /// <exception cref=""MissingMethodException""></exception>
        /// <exception cref=""System.Runtime.InteropServices.COMException""></exception>
        /// <exception cref=""TypeLoadException""></exception>
        public void ValidateAndContinueWith() 
        {
            Activator.CreateInstance(typeof(String));
        }
    }

    class Program 
    {
        static void Main(string[] args) 
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = LiskovViolationsMemberAnalyzer.InterfaceViolationsDiagnosticId,
                Message = String.Format(LiskovViolationsMemberAnalyzer.InterfaceViolationsMessageFormat, "ValidateAndContinueWith", "TargetInvocationException, MethodAccessException, MemberAccessException, InvalidComObjectException, MissingMethodException, COMException, TypeLoadException", "IAbstractValidateur"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 23, 21)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
using System;

namespace ConsoleApplication1 
{
    interface IAbstractValidateur 
    {
        void ValidateAndContinueWith();
    }

    class AbstractValidateur : IAbstractValidateur 
    {
        /// <summary>
        /// ValidateAndContinueWith
        /// </summary>
        /// <exception cref=""System.Reflection.TargetInvocationException"">Ignore.</exception>
        /// <exception cref=""MethodAccessException"">Ignore.</exception>
        /// <exception cref=""MemberAccessException"">Ignore.</exception>
        /// <exception cref=""System.Runtime.InteropServices.InvalidComObjectException"">Ignore.</exception>
        /// <exception cref=""MissingMethodException"">Ignore.</exception>
        /// <exception cref=""System.Runtime.InteropServices.COMException"">Ignore.</exception>
        /// <exception cref=""TypeLoadException"">Ignore.</exception>
        public void ValidateAndContinueWith() 
        {
            Activator.CreateInstance(typeof(String));
        }
    }

    class Program 
    {
        static void Main(string[] args) 
        {
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
            return new LiskovViolationsMemberAnalyzer();
        }
    }
}
