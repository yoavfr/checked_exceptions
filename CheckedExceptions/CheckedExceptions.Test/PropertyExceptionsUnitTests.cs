using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CheckedExceptions.Test
{
    [TestClass]
    public class PropertyExceptionsUnitTests : CodeFixVerifier
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
            public int Foo
            {
                get
                {
                    return 1;
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
            public int Foo
            {
                get
                {
                    try
                    {
                        throw new FieldAccessException();
                    }
                    catch (Exception)
                    {
                    }
                    return 1;
                }
            }
        }
    }";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestExceptionIsDeclared
        /// </summary>
        [TestMethod]
        public void TestExceptionIsDeclared()
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
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        /// <summary>
        /// TestExceptionIsDeclaredMultiLine
        /// </summary>
        [TestMethod]
        public void TestExceptionIsDeclaredMultiLine()
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
        /// <exception cref=""FieldAccessException"">
        /// Get.
        /// </exception>
        public int Foo
        {
            get
            {
                throw new FieldAccessException();
            }
        }
    }
}";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestExceptionInArrowExpressionIsDeclared()
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
        /// <exception cref=""FieldAccessException"">Get.</exception>
        public static int Foo => throw new FieldAccessException();
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
        public int Foo
        {
            get
            {
                try
                {
                    throw new Exception();
                }
                catch
                {
                }
                return 1;
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
        public int Foo
        {
            get
            {
                throw new Exception();
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = PropertyExceptionsAnalyzer.GetterExceptionsDiagnosticId,
                Message = string.Format(PropertyExceptionsAnalyzer.GetterExceptionsMessageFormat, "Exception"),
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
        public int Foo
        {
            get
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);

            fixtestDeclare = @"
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
        /// <exception cref=""Exception"">Get.</exception>
        public int Foo
        {
            get
            {
                throw new Exception();
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);
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
        public int Foo
        {
            get
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
    }
}";
            var expected = new DiagnosticResult
            {
                Id = PropertyExceptionsAnalyzer.GetterExceptionsDiagnosticId,
                Message = String.Format(PropertyExceptionsAnalyzer.GetterExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 23, 21)
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
        public int Foo
        {
            get
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception)
                {
                    try
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                    }
                }
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
        public int Foo
        {
            get
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
    }
}";
            var expected = new DiagnosticResult
            {
                Id = PropertyExceptionsAnalyzer.GetterExceptionsDiagnosticId,
                Message = String.Format(PropertyExceptionsAnalyzer.GetterExceptionsMessageFormat, "Exception"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 19, 21)
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
        public int Foo
        {
            get
            {
                try
                {
                    throw new Exception();
                }
                catch (FieldAccessException)
                {
                }
                catch (Exception)
                {
                }
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestUnahndledException
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionWithWhitespace()
        {
            var test = @"
namespace ConsoleApplication1
{
    class HelloWorld
    {

        public int Foo
        {
            get
            {
                throw new System.FieldAccessException();
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = PropertyExceptionsAnalyzer.GetterExceptionsDiagnosticId,
                Message = String.Format(PropertyExceptionsAnalyzer.GetterExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
namespace ConsoleApplication1
{
    class HelloWorld
    {

        public int Foo
        {
            get
            {
                try
                {
                    throw new System.FieldAccessException();
                }
                catch (System.FieldAccessException)
                {
                }
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);

            fixtestDeclare = @"
namespace ConsoleApplication1
{
    class HelloWorld
    {

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""System.FieldAccessException"">Get.</exception>
        public int Foo
        {
            get
            {
                throw new System.FieldAccessException();
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);

            fixtestDeclare = @"
namespace ConsoleApplication1
{
    class HelloWorld
    {

        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""System.FieldAccessException"">Get. Ignore.</exception>
        public int Foo
        {
            get
            {
                throw new System.FieldAccessException();
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 2);
        }

        /// <summary>
        /// TestUnahndledExceptionInCatchFixed
        /// </summary>
        [TestMethod]
        public void TestUnahndledExceptionDeclared()
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
        /// <exception cref=""FieldAccessException""></exception>
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
                Id = PropertyExceptionsAnalyzer.GetterExceptionsDiagnosticId,
                Message = string.Format(PropertyExceptionsAnalyzer.GetterExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                             new[] {
                            new DiagnosticResultLocation("Test0.cs", 21, 17)
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
        /// <exception cref=""FieldAccessException""></exception>
        public int Foo
        {
            get
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
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);
        }

        /// <summary>
        /// TestThrownExceptionInExceptionBody
        /// </summary>
        [TestMethod]
        public void TestThrownExceptionInExceptionBody()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    public class HelloWorld
    {
        public static int Foo => throw new FieldAccessException();
    }
}";
            var expected = new DiagnosticResult
            {
                Id = PropertyExceptionsAnalyzer.ExpressionBodyExceptionsDiagnosticId,
                Message = String.Format(PropertyExceptionsAnalyzer.ExpressionBodyExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 34)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        /// <summary>
        /// TestUnhandledExceptionInExpressionBody
        /// </summary>
        [TestMethod]
        public void TestUnhandledExceptionInExpressionBody()
        {
            var test = @"
namespace ConsoleApplication1
{
    public class HelloWorld
    {
        public static int Foo => Bar();

        /// <summary>
        /// Bar
        /// </summary>
        /// <exception cref=""System.FieldAccessException""></exception>
        public int Bar()
        {
            throw new System.FieldAccessException();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = PropertyExceptionsAnalyzer.ExpressionBodyExceptionsDiagnosticId,
                Message = String.Format(PropertyExceptionsAnalyzer.ExpressionBodyExceptionsMessageFormat, "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 9)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtestDeclare = @"
namespace ConsoleApplication1
{
    public class HelloWorld
    {
        /// <summary>
        /// Foo
        /// </summary>
        /// <exception cref=""System.FieldAccessException"">Get.</exception>
        public static int Foo => Bar();

        /// <summary>
        /// Bar
        /// </summary>
        /// <exception cref=""System.FieldAccessException""></exception>
        public int Bar()
        {
            throw new System.FieldAccessException();
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);
        }

        /// <summary>
        /// TestUnahndledSetterException
        /// </summary>
        [TestMethod]
        public void TestUnahndledSetterException()
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
        public int Foo
        {
            set
            {
                throw new FieldAccessException();
            }
        }
    }
}";

            var expected =
                new DiagnosticResult
                {
                    Id = PropertyExceptionsAnalyzer.SetterExceptionsDiagnosticId,
                    Message = string.Format(PropertyExceptionsAnalyzer.SetterExceptionsMessageFormat, "FieldAccessException"),
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
        public int Foo
        {
            set
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
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 0);

            fixtestDeclare = @"
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
        /// <exception cref=""FieldAccessException"">Set.</exception>
        public int Foo
        {
            set
            {
                throw new FieldAccessException();
            }
        }
    }
}";
            VerifyCSharpFix(test, fixtestDeclare, 1);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new PropertyExceptionsCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PropertyExceptionsAnalyzer();
        }
    }
}
