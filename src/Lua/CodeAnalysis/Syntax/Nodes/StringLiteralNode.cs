namespace Lua.CodeAnalysis.Syntax.Nodes;

public record StringLiteralNode(string Text, SourcePosition Position) : ExpressionNode(Position)
{
    public override TResult Accept<TContext, TResult>(ISyntaxNodeVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.VisitStringLiteralNode(this, context);
    }
}