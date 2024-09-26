namespace Lua.CodeAnalysis.Syntax.Nodes;

public record TableMethodDeclarationStatementNode(IdentifierNode[] MemberPath, IdentifierNode[] ParameterNodes, StatementNode[] Nodes, bool HasVariableArguments, bool HasSelfParameter, SourcePosition Position) : StatementNode(Position)
{
    public override TResult Accept<TContext, TResult>(ISyntaxNodeVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.VisitTableMethodDeclarationStatementNode(this, context);
    }
}