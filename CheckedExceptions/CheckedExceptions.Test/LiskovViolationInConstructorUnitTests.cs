using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CheckedExceptions.Test
{
    [TestClass]
    public class LiskovViolationInConstructorUnitTests : CodeFixVerifier
    {
        /// <summary>
        /// TestConstructorWithException
        /// </summary>
        [TestMethod]
        public void TestConstructorWithException()
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
            public HelloWorldBase()
            {
            }
        }
        
        public class HelloWorld : HelloWorldBase
        {   
            /// <summary>
            /// ctor
            /// </summary>
            /// <param name=""i""></param>
            /// <exception cref=""FieldAccessException""></exception>
            public HelloWorld()
            {
                throw new FieldAccessException;
            }
        }
    }";
            // nothing should be flagged
            VerifyCSharpDiagnostic(test);
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
