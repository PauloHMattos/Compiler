// namespace Compiler.CodeAnalysis.Syntax
// {
//     public sealed partial class AssignmentExpressionSyntax : ExpressionSyntax
//     {
//         public SyntaxToken IdentifierToken { get; }
//         public SyntaxToken AssignmentToken { get; }
//         public ExpressionSyntax Expression { get; }
//         public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;

//         internal AssignmentExpressionSyntax(SyntaxTree syntaxTree, 
//             SyntaxToken identifierToken, 
//             SyntaxToken assignmentToken, 
//             ExpressionSyntax expression)
//             : base(syntaxTree)
//         {
//             IdentifierToken = identifierToken;
//             AssignmentToken = assignmentToken;
//             Expression = expression;
//         }
//     }
// }