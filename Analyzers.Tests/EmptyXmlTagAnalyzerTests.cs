using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Analyzers.EmptyXmlTagAnalyzer>;

namespace Analyzers.Tests
{
    public class EmptyXmlTagAnalyzerTests
    {
        private const string RuleId = EmptyXmlTagAnalyzer.RuleId;

        [Fact]
        public async Task Finds_DiagnosticResult_With_Newlines_In_Xml_Tag()
        {
            const string test = @"
/// <summary>
///
/// </summary>
class C
{
}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, 5, 4, 15)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Finds_DiagnosticResult_With_Empty_Xml_Tag()
        {
            const string test = @"
/// <summary />
class C
{
}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, 5, 2, 16)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Finds_DiagnosticResult_With_Not_Filled_Xml_Tag()
        {
            const string test = @"
/// <summary></summary>
class C
{
}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, 5, 2, 24)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Class(string visibility)
        {
            string test = $@"
/// <summary />
{visibility} class C {{}}";

            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, 5, 2, 16)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Class_In_Class(string visibility)
        {
            string test = $@"
class C 
{{
    /// <summary />
    {visibility} class Cc {{}}
}}";

            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Enum(string visibility)
        {
            string test = $@"
/// <summary></summary>
{visibility} enum E {{}}";

            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, 5, 2, 24)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Interface(string visibility)
        {
            string test = $@"
/// <summary></summary>
{visibility} interface I {{}}";

            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, 5, 2, 24)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Struct(string visibility)
        {
            string test = $@"
/// <summary></summary>
{visibility} struct S {{}}";

            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, 5, 2, 24)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public", true)]
        [InlineData("private", false)]
        [InlineData("protected", true)]
        [InlineData("internal", true)]
        [InlineData("protected internal", true)]
        [InlineData("private protected", true)]
        public async Task Finds_DiagnosticResult_On_Method(string visibility, bool findsError)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} void M(){{}}
}}";
            if (findsError)
            {
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }

        [Theory]
        [InlineData("public", true)]
        [InlineData("private", false)]
        [InlineData("protected", true)]
        [InlineData("internal", true)]
        [InlineData("protected internal", true)]
        [InlineData("private protected", true)]
        public async Task Finds_DiagnosticResult_On_Property(string visibility, bool findsError)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} int P {{ get; set; }}
}}";
            if (findsError)
            {
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }

        [Theory]
        [InlineData("public", true)]
        [InlineData("private", false)]
        [InlineData("protected", true)]
        [InlineData("internal", true)]
        [InlineData("protected internal", true)]
        [InlineData("private protected", true)]
        public async Task Finds_DiagnosticResult_On_Constructor(string visibility, bool findsError)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} C(){{}}
}}";
            if (findsError)
            {
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }

        [Theory]
        [InlineData("public", true)]
        [InlineData("private", false)]
        [InlineData("protected", true)]
        [InlineData("internal", true)]
        [InlineData("protected internal", true)]
        [InlineData("private protected", true)]
        public async Task Finds_DiagnosticResult_On_EventField(string visibility, bool findsError)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} event System.Action Event;
}}";
            if (findsError)
            {
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }
        
        [Theory]
        [InlineData("public", true)]
        [InlineData("private", false)]
        [InlineData("protected", true)]
        [InlineData("internal", true)]
        [InlineData("protected internal", true)]
        [InlineData("private protected", true)]
        public async Task Finds_DiagnosticResult_On_Event(string visibility, bool findsError)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} event System.Action Event{{ add{{}} remove{{}} }}
}}";
            if (findsError)
            {
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }
        
        [Theory]
        [InlineData("public", true)]
        [InlineData("private", false)]
        [InlineData("protected", true)]
        [InlineData("internal", true)]
        [InlineData("protected internal", true)]
        [InlineData("private protected", true)]
        public async Task Finds_DiagnosticResult_On_Delegate(string visibility, bool findsError)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} delegate void Delegate();
}}";
            if (findsError)
            {
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }
        
        [Fact]
        public async Task Finds_DiagnosticResult_On_Destructor()
        {
            const string test = @"
class C
{
    /// <summary />
    ~C(){}
}";
            
            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Theory]
        [InlineData("public", true)]
        [InlineData("private", false)]
        [InlineData("protected", true)]
        [InlineData("internal", true)]
        [InlineData("protected internal", true)]
        [InlineData("private protected", true)]
        public async Task Finds_DiagnosticResult_On_Indexer(string visibility, bool findsError)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} int this[int a] {{ get => a; }}
}}";
            if (findsError)
            {
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }

        [Fact]
        public async Task Finds_DiagnosticResult_On_Operator()
        {
            string test = @"
class C
{
    /// <summary />
    public static bool operator==(C current, C other) { return true; }

    public static bool operator!=(C current, C other) { return false; }
}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Fact]
        public async Task Finds_DiagnosticResult_On_Enum_Member()
        {
            const string test = @"
enum E
{
    /// <summary />
    Member
}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Does_Not_Find_DiagnosticResult_On_Field(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} int f;
}}";

            await Verify.VerifyAnalyzerAsync(test);
        }
    }
}