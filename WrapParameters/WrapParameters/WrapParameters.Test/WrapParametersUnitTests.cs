using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = WrapParameters.Test.CSharpCodeFixVerifier<
    WrapParameters.WrapParametersAnalyzer,
    WrapParameters.WrapParametersCodeFixProvider>;

namespace WrapParameters.Test
{
    [TestClass]
    public class WrapParametersUnitTest
    {
        private readonly string _testCode = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        static class DemoAnalyzers
        {
            public static void DemoWrapParameters(int parameter1,
                string parameter2,
                object parameter3,
                float parameter4,
                string parameter5,)
            {
        
            }
        }
    }
";

        private readonly string _fixedCode = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        static class DemoAnalyzers
        {
            public static void DemoWrapParameters(int parameter1,
                string parameter2,
                object parameter3,
                float parameter4,
                string parameter5)
            {
        
            }
        }
    }
";

        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestAnalyzer()
        {
            await VerifyCS.VerifyAnalyzerAsync(_testCode);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestCodeFix()
        {
            var expected = VerifyCS.Diagnostic("DPA0001").WithLocation(13, 32).WithArguments("DemoWrapParameters"); //12,32
            await VerifyCS.VerifyCodeFixAsync(_testCode, expected, _fixedCode);
        }
    }
}
