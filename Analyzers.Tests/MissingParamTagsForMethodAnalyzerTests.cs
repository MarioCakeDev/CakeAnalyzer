using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Analyzers.MissingParamTagsForMethodAnalyzer>;

namespace Analyzers.Tests
{
    public class MissingParamTagsForMethodAnalyzerTests
    {
        private const string RuleId = MissingParamTagsForMethodAnalyzer.RuleId;

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Class(string visibility)
        {
            string test = $@"
{visibility} class C {{}}";
            
            int startColumn = visibility.Length + 8;
            int endColumn = startColumn + 1;
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, startColumn, 2, endColumn)
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
/// <summary />
class C
{{
    {visibility} class Cc {{}}
}}";
            
            int startColumn = visibility.Length + 12;
            int endColumn = startColumn + 2;
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(5, startColumn, 5, endColumn)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Enum(string visibility)
        {
            string test = $@"
{visibility} enum E {{}}";

            int startColumn = visibility.Length + 7;
            int endColumn = startColumn + 1;
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, startColumn, 2, endColumn)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Interface(string visibility)
        {
            string test = $@"
{visibility} interface I {{
    /// <summary>
    ///
    /// </summary>
    void SetScenarioItem(int amount, string product, decimal price);
}}";

            int startColumn = visibility.Length + 12;
            int endColumn = startColumn + 1;
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, startColumn, 2, endColumn)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Struct(string visibility)
        {
            string test = $@"
{visibility} struct S {{}}";

            int startColumn = visibility.Length + 9;
            int endColumn = startColumn + 1;
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(2, startColumn, 2, endColumn)
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
            string test = $@" /// <summary />
class C
{{
    {visibility} void M(){{}}
}}";
            
            if (findsError)
            {
                int startColumn = visibility.Length + 11;
                int endColumn = startColumn + 1;
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn));
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
            string test = $@" /// <summary />
class C
{{
    {visibility} int P {{ get; set; }}
}}";
            if (findsError)
            {
                int startColumn = visibility.Length + 10;
                int endColumn = startColumn + 1;
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn));
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
            string test = $@" /// <summary />
class C
{{
    {visibility} C(){{}}
}}";
            if (findsError)
            {
                int startColumn = visibility.Length + 6;
                int endColumn = startColumn + 1;
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn));
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
            string test = $@" /// <summary />
class C
{{
    {visibility} event System.Action Event;
}}";
            
            if (findsError)
            {
                int startColumn = visibility.Length + 26;
                int endColumn = startColumn + 5;
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn));
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
            string test = $@" /// <summary />
class C
{{
    {visibility} event System.Action Event{{ add{{}} remove{{}} }}
}}";
            if (findsError)
            {
                int startColumn = visibility.Length + 26;
                int endColumn = startColumn + 5;
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn));
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
            string test = $@" /// <summary />
class C
{{
    {visibility} delegate void Delegate();
}}";
            if (findsError)
            {
                int startColumn = visibility.Length + 20;
                int endColumn = startColumn + 8;
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }
        
        [Fact]
        public async Task Finds_DiagnosticResult_On_Destructor()
        {
            const string test = @" /// <summary />
class C
{
    ~C(){}
}";
            
            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 6, 4, 7));
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
            string test = $@" /// <summary />
class C
{{
    {visibility} int this[int a] {{ get => a; }}
}}";
            if (findsError)
            {
                int startColumn = visibility.Length + 10;
                int endColumn = startColumn + 4;
                await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn));
            }
            else
            {
                await Verify.VerifyAnalyzerAsync(test);
            }
        }

        [Fact]
        public async Task Finds_DiagnosticResult_On_Operator()
        {
            string test = @"/// <summary />
class C
{
    public static bool operator==(C current, C other) { return true; }

    /// <summary />
    public static bool operator!=(C current, C other) { return false; }
}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 24, 4, 32));
        }

        [Fact]
        public async Task Finds_DiagnosticResult_On_Enum_Member()
        {
            const string test = @"/// <summary />
enum E
{
    Member
}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 5, 4, 11));
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
            string test = $@"/// <summary />
class C
{{
    {visibility} int f;
}}";

            await Verify.VerifyAnalyzerAsync(test);
        }
        
        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Does_Not_Find_DiagnosticResult_On_Method_With_Fact_Attribute(string visibility)
        {
            string test = $@"/// <summary />
class C
{{
    [Fact]
    {visibility} void M(){{}}
}}

/// <summary />
class FactAttribute : System.Attribute{{}}
";

            await Verify.VerifyAnalyzerAsync(test);
        }
        
        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Does_Not_Find_DiagnosticResult_On_Method_With_Theory_Attribute(string visibility)
        {
            string test = $@"/// <summary />
class C
{{
    [Theory]
    {visibility} void M(){{}}
}}

/// <summary />
class TheoryAttribute : System.Attribute{{}}
";

            await Verify.VerifyAnalyzerAsync(test);
        }
    }
}