using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Analyzers.SimpleXmlParamAnalyzer>;

namespace Analyzers.Tests
{
    public class SimpleXmlParamAnalyzerTests
    {
        private const string RuleId = SimpleXmlParamAnalyzer.RuleId;

        [Theory]
        [InlineData("thisIsAParameterName")]
        [InlineData("this Is A Parameter Name")]
        [InlineData("THIS IS A PARAMETER NAME")]
        [InlineData("this is a parameter name")]
        [InlineData("  this is a parameter name  ")]
        [InlineData("\tthis is a\tparameter name  ")]
        [InlineData("thisIsA ParameterName")]
        public async Task Finds_DiagnosticResult_With_Simple_Xml_Comments(string paramComment)
        {
            string test = $@"
class C 
{{
    /// <param name=""thisIsAParameterName"">{paramComment}</param>
    void M(string thisIsAParameterName){{}}
}}";
            const int startColumn = 9;
            int endColumn = paramComment.Length + 52;
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData("thisIsAParameterName")]
        [InlineData("this Is A Parameter Name")]
        [InlineData("THIS IS A PARAMETER NAME")]
        [InlineData("this is a parameter name")]
        [InlineData("  this is a parameter name  ")]
        [InlineData("\tthis is a\tparameter name  ")]
        [InlineData("thisIsA ParameterName")]
        public async Task Finds_DiagnosticResult_With_Simple_Multiline_Xml_Comments(string paramComment)
        {
            string test = $@"
class C 
{{
    /** <param name=""thisIsAParameterName"">{paramComment}</param> **/
    void M(string thisIsAParameterName){{}}
}}";
            const int startColumn = 9;
            int endColumn = paramComment.Length + 52;
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(4, startColumn, 4, endColumn)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
        
        [Theory]
        [InlineData("thisIsAParameterName")]
        [InlineData("this Is A Parameter Name")]
        [InlineData("THIS IS A PARAMETER NAME")]
        [InlineData("this is a parameter name")]
        [InlineData("  this is a parameter name  ")]
        [InlineData("\tthis is a\tparameter name  ")]
        [InlineData("thisIsA ParameterName")]
        public async Task Finds_DiagnosticResult_With_Simple_Xml_Comments_Over_Multiple_Lines(string paramComment)
        {
            string test = $@"
class C 
{{
    /// <param name=""thisIsAParameterName"">
    /// {paramComment}
    /// </param>
    void M(string thisIsAParameterName){{}}
}}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic(RuleId).WithSpan(4, 9, 6, 17)
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}