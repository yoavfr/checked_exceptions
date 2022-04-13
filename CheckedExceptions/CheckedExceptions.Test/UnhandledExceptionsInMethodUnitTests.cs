using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CheckedExceptions.Test
{
    [TestClass]
    public class UnhandledExceptionsInMethodUnitTests : CodeFixVerifier
    {
        /// <summary>
        /// TestEmptyDocument
        /// </summary>
        [TestMethod]
        public void TestEmptyDocument()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestEmptyClass
        /// </summary>
        [TestMethod]
        public void TestEmptyClass()
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
            public int Foo()
            {
                return 1;
            }
        }
    }";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// Exception base is caught 
        /// </summary>
        [TestMethod]
        public void TestExceptionBaseIsCaught()
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
            public int Foo()
            {
                try
                {
                    throw new FieldAccessException();
                }
                catch (Exception)
                {
                }
            }
        }
    }";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// Exception base is caught 
        /// </summary>
        [TestMethod]
        public void TestExceptionIsCaught()
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
            /// <exception cref=""Exception""/>
            public void Foo() => throw new Exception();

            public void Bar()
            {
                try
                {
                    Foo();
                }
                catch (Exception)
                {
                }
            }
        }
    }";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestAnonymousCatchNoRethrow 
        /// </summary>
        [TestMethod]
        public void TestAnonymousCatchNoRethrow()
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
            public int Foo()
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
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

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
            public int Foo()
            {
                throw new Exception();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""Exception""></exception>
            public int Foo()
            {
                throw new Exception();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledException
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionExternalCode()
        {
            var test = @"
using System.IO;
namespace SillyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            File.Open(""kuku"", FileMode.Open);
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
using System.IO;
namespace SillyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            File.Open(""kuku"", FileMode.Open);
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledException
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionPrivateMethod()
        {
            ShortCommentConfiguration.Instance.ForPrivate = true;
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
            private int Foo()
            {
                throw new Exception();
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
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
            /// <exception cref=""Exception""></exception>
            private int Foo()
            {
                throw new Exception();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestGetterInvocation
        /// </summary>
        [TestMethod]
        public void TestGetterInvocation()
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
        static void Main(string[] args)
        {
            var x = new HelloWorld().Foo;
        }

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""Exception"">Getter.</exception>
        public int Foo
        {
            get
            {
                throw new Exception();
            }
            set
            {
                
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = string.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        /// <summary>
        /// TestSetterInvocation
        /// </summary>
        [TestMethod]
        public void TestSetterInvocation()
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
        static void Main(string[] args)
        {
            new HelloWorld().Foo = 1;
        }

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""Exception"">Setter.</exception>
        public int Foo
        {
            get
            {
                return 1;
            }
            set
            {
                throw new Exception();
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = string.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        /// <summary>
        /// TestGetterInvocationSetterThrows
        /// </summary>
        [TestMethod]
        public void TestGetterInvocationSetterThrows()
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
        static void Main(string[] args)
        {
            var x = Foo;
        }

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""Exception"">Setter.</exception>
        public int Foo
        {
            get
            {
                return 1;
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
        /// TestRethrownException
        /// </summary>
        [TestMethod]
        public void TestRethrownException()
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
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 21, 21)
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""Exception""></exception>
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestAnonymousCatchRethrownException
        /// </summary>
        [TestMethod]
        public void TestAnonymousCatchRethrownException()
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
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch
                {
                    throw;
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 21)
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""Exception""></exception>
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch
                {
                    throw;
                }
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestExceptionFilters
        /// </summary>
        [TestMethod]
        public void TestExceptionFilters()
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
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception) when (Exception is System.IO.FileNotFoundException)
                {
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 21)
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""Exception""></exception>
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception) when (Exception is System.IO.FileNotFoundException)
                {
                }
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledExceptionBaseIsThrown
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionBaseIsThrown()
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
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch (FieldAccessException)
                {
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 21)
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""Exception""></exception>
            public int Foo()
            {
                try
                {
                    throw new Exception();
                }
                catch (FieldAccessException)
                {
                }
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestIgnoredException
        /// </summary>
        [TestMethod]
        public void TestIgnoredException()
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException"">Ignore.</exception>
            public int Foo()
            {
                Console.WriteLine(Bar());
                return 1;
            }

            /// <summary>
            /// Bar
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException""></exception>
            public string Bar()
            {
                return ""Hello"";
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestUnahndledException
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

            public int Foo()
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException""></exception>
            public int Foo()
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            public int Foo()
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException""></exception>
            public int Foo()
            {
                throw new FieldAccessException();
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledExceptionInCatch
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionInCatch()
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
            public int Foo()
            {
                try
                {
                }
                catch (Exception)
                {
                    throw new FieldAccessException();
                }
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
                            new DiagnosticResultLocation("Test0.cs", 20, 21)
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException""></exception>
            public int Foo()
            {
                try
                {
                }
                catch (Exception)
                {
                    throw new FieldAccessException();
                }
            }
        }
    }";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledExceptionInCatchFixed
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionInCatchFixed()
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
            /// Foo
            /// </summary>
            /// <returns></returns>
            /// <exception cref=""FieldAccessException""></exception>
            public int Foo()
            {
                try
                {
                }
                catch (Exception)
                {
                    throw new FieldAccessException();
                }
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestUnhandledExceptionAddTryCatch
        /// </summary>
        [TestMethod]
        public void TestUnhandledExceptionAddTryCatch()
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
        public int Foo()
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
                            new DiagnosticResultLocation("Test0.cs", 15, 13)
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
        public int Foo()
        {
            try
            {
                throw new FieldAccessException();
            }
            catch (FieldAccessException)
            {
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);
        }

        /// <summary>
        /// TestUnhandledExceptionAddTryCatchOnInvocation
        /// </summary>
        [TestMethod]
        public void TestUnhandledExceptionAddTryCatchOnInvocation()
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
        public int Foo()
        {
            return Bar()
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref=""FieldAccessException""></exception>
        public int Bar()
        {
            return 1;
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
                            new DiagnosticResultLocation("Test0.cs", 15, 13)
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
        public int Foo()
        {
            try
            {
                return Bar()
            }
            catch (FieldAccessException)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref=""FieldAccessException""></exception>
        public int Bar()
        {
            return 1;
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);
        }

        /// <summary>
        /// TestUnhandledExceptionAppendCatch
        /// </summary>
        [TestMethod]
        public void TestUnhandledExceptionAppendCatch()
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
        public int Foo()
        {
            try
            {
                throw new FieldAccessException();
            }
            catch (NullReferenceException)
            {
            }
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
                            new DiagnosticResultLocation("Test0.cs", 17, 17)
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
        public int Foo()
        {
            try
            {
                throw new FieldAccessException();
            }
            catch (NullReferenceException)
            {
            }
            catch (FieldAccessException)
            {
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);
        }

        /// <summary>
        /// TestUnhandledExceptionInLambdaAppendCatch
        /// </summary>
        [TestMethod]
        public void TestUnhandledExceptionInLambdaAppendCatch()
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
    public class HelloWorld
    {
        /// <summary>
        /// Bar
        /// </summary>
        /// <param name=""i""></param>
        /// <returns></returns>
        /// <exception cref=""FieldAccessException""></exception>
        public int Bar(int i)
        {
            return i + 1;
        }

        public void Foo()
        {
            var e = new int[3];
            e.Select(x => Bar(x));
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
                            new DiagnosticResultLocation("Test0.cs", 27, 13)
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
    public class HelloWorld
    {
        /// <summary>
        /// Bar
        /// </summary>
        /// <param name=""i""></param>
        /// <returns></returns>
        /// <exception cref=""FieldAccessException""></exception>
        public int Bar(int i)
        {
            return i + 1;
        }

        public void Foo()
        {
            var e = new int[3];
            try
            {
                e.Select(x => Bar(x));
            }
            catch (FieldAccessException)
            {
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);
        }

        /// <summary>
        /// TestUnhandledExceptionInLambdaAppendCatch
        /// </summary>
        [TestMethod]
        public void TestUnhandledExceptionNotIncluded()
        {
            var test = @"
namespace ConsoleApplication1
{
    public class HelloWorld
    {
        /// <summary>
        /// Bar
        /// </summary>
        /// <param name=""i""></param>
        /// <returns></returns>
        /// <exception cref=""System.FieldAccessException""></exception>
        public void Bar(int i)
        {
        }

        public void Foo()
        {
            Bar(1);
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
                            new DiagnosticResultLocation("Test0.cs", 18, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
namespace ConsoleApplication1
{
    public class HelloWorld
    {
        /// <summary>
        /// Bar
        /// </summary>
        /// <param name=""i""></param>
        /// <returns></returns>
        /// <exception cref=""System.FieldAccessException""></exception>
        public void Bar(int i)
        {
        }

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""System.FieldAccessException""></exception>
        public void Foo()
        {
            Bar(1);
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestSetterThrowsGetterInvoked
        /// </summary>
        [TestMethod]
        public void TestSetterThrowsGetterInvoked()
        {
            var test = @"
using System;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            int x = new Program().Foo;
        }

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Set.</exception>
        public int Foo
        {
            get
            {
                return 1;
            }
            set
            {
                throw new FieldAccessException();
            }
        }
    }
}
";
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestUnhandledExceptionInPropertyWithBothGetAndSet
        /// </summary>
        [TestMethod]
        public void TestUnhandledExceptionInPropertyWithBothGetAndSet()
        {
            var test = @"
using System;
namespace ConsoleApplication1
{
    class HelloWorld
    {
        public static void Main()
        {
            new HelloWorld().Foo++;
        }

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Set.</exception>
        /// <exception cref=""Exception"">Get.</exception>
        public int Foo
        {
            get
            {
                throw new Exception();
            }
            set
            {
                throw new FieldAccessException();
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "FieldAccessException, Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
using System;
namespace ConsoleApplication1
{
    class HelloWorld
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <exception cref=""FieldAccessException""></exception>
        /// <exception cref=""Exception""></exception>
        public static void Main()
        {
            new HelloWorld().Foo++;
        }

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""FieldAccessException"">Set.</exception>
        /// <exception cref=""Exception"">Get.</exception>
        public int Foo
        {
            get
            {
                throw new Exception();
            }
            set
            {
                throw new FieldAccessException();
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestExceptionThrowOfReturnedObject
        /// </summary>
        [TestMethod]
        public void TestExceptionThrowOfReturnedObject()
        {
            var test = @"
using System;
namespace HelloWorld
{
    class Program2
    {
        public void DoSomething()
        {
            throw ConvertToException(new FieldAccessException());
        }

        public Exception ConvertToException(Exception e)
        {
            return new Exception();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 19)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
using System;
namespace HelloWorld
{
    class Program2
    {
        /// <summary>
        /// DoSomething
        /// </summary>
        /// <exception cref=""Exception""></exception>
        public void DoSomething()
        {
            throw ConvertToException(new FieldAccessException());
        }

        public Exception ConvertToException(Exception e)
        {
            return new Exception();
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestExceptionRethrowOfReturnedObject
        /// </summary>
        [TestMethod]
        public void TestExceptionRethrowOfReturnedObject()
        {
            var test = @"
using System;
namespace HelloWorld
{
    class Program2
    {
        public void DoSomething()
        {
            try
            {
                DoSomething();
            }
            catch (FieldAccessException e)
            {
                throw ConvertToException(e);
            }
        }

        public Exception ConvertToException(Exception e)
        {
            return new Exception();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 23)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
using System;
namespace HelloWorld
{
    class Program2
    {
        /// <summary>
        /// DoSomething
        /// </summary>
        /// <exception cref=""Exception""></exception>
        public void DoSomething()
        {
            try
            {
                DoSomething();
            }
            catch (FieldAccessException e)
            {
                throw ConvertToException(e);
            }
        }

        public Exception ConvertToException(Exception e)
        {
            return new Exception();
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestInheritDocInterfaceHandled
        /// </summary>
        [TestMethod]
        public void TestInheritDocInterfaceHandled()
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
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""Exception""></exception>
        int Foo();
    }
    
    class HelloWorld : IHelloWorld
    {   
        /// <inheritdoc/>
        public int Foo()
        {
            throw new Exception();
        }
    }
}";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestInheritDocInterfaceNotHandled
        /// </summary>
        [TestMethod]
        public void TestInheritDocInterfaceNotHandled()
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
        /// <summary>
        /// Foo
        /// </summary>
        int Foo();
    }
    
    class HelloWorld : IHelloWorld
    {   
        /// <inheritdoc/>
        public int Foo()
        {
            throw new Exception();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 24, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        /// <summary>
        /// TestInheritDocBaseHandled
        /// </summary>
        [TestMethod]
        public void TestInheritDocBaseHandled()
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
    abstract class HelloWorldBase
    {
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""Exception""></exception>
        public abstract void Foo();
    }
    
    class HelloWorld : HelloWorldBase
    {   
        /// <inheritdoc/>
        public override int Foo()
        {
            throw new Exception();
        }
    }
}";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestInheritDocBaseNotHandled
        /// </summary>
        [TestMethod]
        public void TestInheritDocBaseNotHandled()
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
    abstract class HelloWorldBase
    {
        /// <summary>
        /// Foo
        /// </summary>
        public abstract void Foo();
    }
    
    class HelloWorld : HelloWorldBase
    {   
        /// <inheritdoc/>
        public override int Foo()
        {
            throw new Exception();
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = UnhandledExceptionsAnalyzer.UnhandledExceptionsDiagnosticId,
                Message = String.Format(UnhandledExceptionsAnalyzer.UnhandledExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 24, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
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
