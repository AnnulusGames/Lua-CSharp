namespace Lua.CodeAnalysis.Syntax.Nodes;

public record GenericForStatementNode(IdentifierNode[] Names, ExpressionNode ExpressionNode, StatementNode[] StatementNodes, SourcePosition Position) : StatementNode(Position)
{
    public override TResult Accept<TContext, TResult>(ISyntaxNodeVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.VisitGenericForStatementNode(this, context);
    }
}