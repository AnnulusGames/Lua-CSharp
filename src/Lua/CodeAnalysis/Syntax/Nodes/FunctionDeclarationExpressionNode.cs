namespace Lua.CodeAnalysis.Syntax.Nodes;

public record FunctionDeclarationExpressionNode(IdentifierNode[] ParameterNodes, SyntaxNode[] Nodes, bool HasVariableArguments, SourcePosition Position) : ExpressionNode(Position)
{
    public override TResult Accept<TContext, TResult>(ISyntaxNodeVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.VisitFunctionDeclarationExpressionNode(this, context);
    }
}