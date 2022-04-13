using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CheckedExceptions.Test
{
    [TestClass]
    public class EventHandlerExceptionsUnitTests : DiagnosticVerifier
    {
        [TestMethod]
        public void TestExceptionDeclaredForHandler()
        {
            var test = @"
using Newtonsoft.Json;
using System;

namespace SillyTest
{
    
    class Program 
    {
        public event EventHandler MyEvent;

        static void Main(string[] args)
        {
            var p = new Program();
            p.MyEvent += OnMyEvent;
        }

        /// <summary>
        /// OnMyEvent
        /// </summary>
        /// <param name=""sender""></param>
        /// <param name=""e""></param>
        /// <exception cref=""FieldAccessException""></exception>
            private static void OnMyEvent(object sender, EventArgs e)
            {
                throw new FieldAccessException();
            }

            protected void TriggerEvent(EventArgs args)
            {
                MyEvent?.Invoke(this, args);
            }
        }
    }
";

            var expected = new DiagnosticResult
            {
                Id = EventHandlerExceptionAnalyzer.EventHandlerExceptionsDiagnosticId,
                Message = String.Format(EventHandlerExceptionAnalyzer.EventHandlerExceptionsMessageFormat, "OnMyEvent", "FieldAccessException"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                new[] {
                         new DiagnosticResultLocation("Test0.cs", 15, 26)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EventHandlerExceptionAnalyzer();
        }
    }
}