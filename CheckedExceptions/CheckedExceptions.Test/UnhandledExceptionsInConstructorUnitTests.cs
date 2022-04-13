using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CheckedExceptions.Test
{
    [TestClass]
    public class UnhandledExceptionsInConstructorUnitTests : CodeFixVerifier
    {
        /// <summary>
        /// TestUnahndledException
        /// </summary>
        [TestMethod]
        public void TestUnahndledException()
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
        class HelloWorld
        {
            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = string.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 17)
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
        class HelloWorld
        {
            /// <summary>
            /// ctor
            /// </summary>
            /// <param name=""i""></param>
            /// <exception cref=""FieldAccessException""></exception>
            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledExceptionWithWhitespace
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionWithWhitespace()
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
        class HelloWorld
        {

            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 17)
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
        class HelloWorld
        {

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name=""i""></param>
            /// <exception cref=""FieldAccessException""></exception>
            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledExceptionAppendComment
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionAppendComment()
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
        class HelloWorld
        {
            /// <summary>
            /// ctor
            /// </summary>
            /// <param name=""i""></param>
            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 19, 17)
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
        class HelloWorld
        {
            /// <summary>
            /// ctor
            /// </summary>
            /// <param name=""i""></param>
            /// <exception cref=""FieldAccessException""></exception>
            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledExceptionAppendCommentWithWhitespace
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionAppendCommentWithWhitespace()
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
        class HelloWorld
        {

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name=""i""></param>
            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 17)
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
        class HelloWorld
        {

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name=""i""></param>
            /// <exception cref=""FieldAccessException""></exception>
            public HelloWorld(int i)
            {
                throw new FieldAccessException();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UnhandledExceptionsCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UnhandledExceptionsAnalyzer();
        }
    }
}
