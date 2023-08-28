using Gamba.Ast;
using Gamba.Interop;
using Gamba.Utility;

namespace Gamba.UnitTests
{
    [TestClass]
    public class CheckLinearTests
    {
        [TestMethod]
        public void TestCheckLinear()
        {
            var datasets = DatasetLoader.GetMbaDatasets();
            foreach(var dataset in datasets)
            {
                foreach(var mbaExpression in dataset.MbaExpressions)
                {
                    Assert.IsTrue(IsLinearityEqual(mbaExpression.StrExpr, mbaExpression.ParsedExpr));
                    Assert.IsTrue(IsLinearityEqual(mbaExpression.StrGroundTruth, mbaExpression.ParsedGroundTruth));
                }
            }
        }

        private bool IsLinearityEqual(string expression, AstNode parsedExpression)
        {
            // Use simba to check if the string representation is linear.
            bool simbaIsLinear = FastSimba.CheckLinear(expression);

            // Use Gamba.NET to check if the parsed expression is linear.
            var classificationMapping = AstClassifier.Classify(parsedExpression);
            bool gambaIsLinear = AstClassifier.IsLinear(classificationMapping[parsedExpression]);

            // If SIMBA returns a different result then us, then something is wrong.
            if (simbaIsLinear != gambaIsLinear)
                return false;

            // Otherwise GAMBA.NET is behaving as expected. Recurse into checking the subexpressions.
            foreach(var (ast, classification) in classificationMapping)
            {
                // Skip if this is the root expression.
                if (ast == parsedExpression)
                    continue;

                // If SIMBA does not report the same result as us then something is wrong.
                if (AstClassifier.IsLinear(classification) != FastSimba.CheckLinear(ast.ToString()))
                    return false;
            }

            return true;
        }
    }
}