using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Borland.CodeAnalyzer.Test
{
    [TestClass]
    public class CommentCodeAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        public async Task Test_On_Correct_Code()
        {
            // Arrange
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
            // test 1234;
        }
    }";

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test);
        }

        [TestMethod]
        public async Task Test_On_Single_Line_Comment()
        {
            // Arrange
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        // var a = 0;
        class TypeName
        {   
            // test 1234;
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.CommentCode,
                    Message = "Comment contains code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 9),
                    }
                };
            var fixtest = @"
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
            // test 1234;
        }
    }";

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [TestMethod]
        public async Task Test_On_Milti_Line_Comment()
        {
            // Arrange
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        /*
        var a = 0;
        */
        class TypeName
        {   
            // test 1234;
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.CommentCode,
                    Message = "Comment contains code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 9),
                    }
                };
            var fixtest = @"
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
            // test 1234;
        }
    }";

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [TestMethod]
        public async Task Test_On_Method_In_Single_Line_Comment()
        {
            // Arrange
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        //void Test()
        //{
        //   var a = 0;
        //}
        class TypeName
        {   
            // test 1234;
        }
    }";
            var expected = 
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.CommentCode,
                    Message = "Comment contains code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 9),
                    }
                };
            var fixtest = @"
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
            // test 1234;
        }
    }";

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
            await VerifyCSharpFixAsync(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CommentCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CommentCodeAnalyzer();
        }
    }
}
