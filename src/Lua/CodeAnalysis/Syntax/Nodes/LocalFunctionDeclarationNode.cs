namespace Lua.CodeAnalysis.Syntax.Nodes;

public record LocalFunctionDeclarationStatementNode(ReadOnlyMemory<char> Name, IdentifierNode[] ParameterNodes, SyntaxNode[] Nodes, bool HasVariableArguments, SourcePosition Position) : FunctionDeclarationStatementNode(Name, ParameterNodes, Nodes, HasVariableArguments, Position)
{
    public override TResult Accept<TContext, TResult>(ISyntaxNodeVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.VisitLocalFunctionDeclarationStatementNode(this, context);
    }
}