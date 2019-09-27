using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Borland.CodeAnalyzer.Test
{
    [TestClass]
    public class LoggingAnalyzerTests : CodeFixVerifier
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
            void MethodName()
            {
                Console.ReadLine();
            }
        }
    }";

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test);
        }

        [TestMethod]
        public async Task Test_On_Normal_Invoke()
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
            void MethodName()
            {
                Console.ReadLine();
                Console.WriteLine();
            }
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.Logging,
                    Message = "Code contains Console.WriteLine code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 16, 17),
                    }
                };

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [TestMethod]
        public async Task Test_On_Namespace_Invoke()
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
            void MethodName()
            {
                Console.ReadLine();
                System.Console.WriteLine();
            }
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.Logging,
                    Message = "Code contains Console.WriteLine code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 16, 17),
                    }
                };

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [TestMethod]
        public async Task Test_On_Global_Invoke()
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
            void MethodName()
            {
                Console.ReadLine();
                global::System.Console.WriteLine();
            }
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.Logging,
                    Message = "Code contains Console.WriteLine code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 16, 17),
                    }
                };

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [TestMethod]
        public async Task Test_On_Static_Invoke()
        {
            // Arrange
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using static System.Console;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            void MethodName()
            {
                Console.ReadLine();
                WriteLine();
            }
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.Logging,
                    Message = "Code contains Console.WriteLine code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 17, 17),
                    }
                };

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [TestMethod]
        public async Task Test_On_AliasNamespace_Invoke()
        {
            // Arrange
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using s = System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            void MethodName()
            {
                Console.ReadLine();
                s.Console.WriteLine();
            }
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.Logging,
                    Message = "Code contains Console.WriteLine code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 17, 17),
                    }
                };

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [TestMethod]
        public async Task Test_On_AliasClass_Invoke()
        {
            // Arrange
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using c = System.Console;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            void MethodName()
            {
                Console.ReadLine();
                c.WriteLine();
            }
        }
    }";
            var expected =
                new DiagnosticResult
                {
                    Id = AnalyzerCodes.DiagnosticIds.Logging,
                    Message = "Code contains Console.WriteLine code.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 17, 17),
                    }
                };

            // Act and Assert
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LoggingAnalyzer();
        }
    }
}
