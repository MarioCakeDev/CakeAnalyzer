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

        [Fact]
        public async Task Does_Not_Find_DiagnosticResult_With_Filled_SingeLine_Comment()
        {
            const string test = @"
/// <summary>ABC</summary>
class C
{
}";

            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task Does_Not_Find_DiagnosticResult_With_Filled_Multiline_Comment()
        {
            const string test = @"
/** <summary>ABC</summary> **/
class C
{
}";

            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task Finds_DiagnosticResult_With_Multiline_Comment()
        {
            const string test = @"
/** <summary></summary> **/
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
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Method(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} void M(){{}}
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }
        
        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Method_In_Interface(string visibility)
        {
            string test = $@"
interface I
{{
    /// <summary />
    {visibility} void M(){{}}
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }
        
        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("internal")]
        public async Task Finds_DiagnosticResult_On_Method_In_Struct(string visibility)
        {
            string test = $@"
struct S
{{
    /// <summary />
    {visibility} void M(){{}}
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Property(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} int P {{ get; set; }}
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Constructor(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} C(){{}}
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_EventField(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} event System.Action Event;
}}";
            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Event(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} event System.Action Event{{ add{{}} remove{{}} }}
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Delegate(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} delegate void Delegate();
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
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
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task Finds_DiagnosticResult_On_Indexer(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} int this[int a] {{ get => a; }}
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
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
        public async Task Finds_DiagnosticResult_On_Field(string visibility)
        {
            string test = $@"
class C
{{
    /// <summary />
    {visibility} int f;
}}";

            await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic(RuleId).WithSpan(4, 9, 4, 20));
        }

        [Fact]
        public async Task Does_Not_Find_DiagnosticResult_On_Empty_InheritDoc_Tag()
        {
            const string test = @"
class C
{
    /// <inheritdoc />
    public int f;
}";

            await Verify.VerifyAnalyzerAsync(test);
        }
    }
}