using Lua.Internal;
using Lua.CodeAnalysis.Syntax;
using Lua.CodeAnalysis.Syntax.Nodes;
using Lua.Runtime;

namespace Lua.CodeAnalysis.Compilation;

public sealed class LuaCompiler : ISyntaxNodeVisitor<ScopeCompilationContext, bool>
{
    public static readonly LuaCompiler Default = new();

    public Chunk Compile(string source, string? chunkName = null)
    {
        return Compile(LuaSyntaxTree.Parse(source, chunkName), chunkName);
    }

    /// <summary>
    /// Returns a compiled chunk of the syntax tree.
    /// </summary>
    public Chunk Compile(LuaSyntaxTree syntaxTree, string? chunkName = null)
    {
        using var context = FunctionCompilationContext.Create(null);

        // set global enviroment upvalue
        context.AddUpValue(new()
        {
            Name = "_ENV".AsMemory(),
            Id = 0,
            Index = -1,
            IsInRegister = false,
        });

        context.ChunkName = chunkName;

        syntaxTree.Accept(this, context.Scope);
        return context.ToChunk();
    }

    // Syntax Tree
    public bool VisitSyntaxTree(LuaSyntaxTree node, ScopeCompilationContext context)
    {
        foreach (var childNode in node.Nodes)
        {
            childNode.Accept(this, context);
        }

        return true;
    }

    // Literals
    public bool VisitNilLiteralNode(NilLiteralNode node, ScopeCompilationContext context)
    {
        context.PushInstruction(Instruction.LoadNil(context.StackPosition, 1), node.Position, true);
        return true;
    }

    public bool VisitBooleanLiteralNode(BooleanLiteralNode node, ScopeCompilationContext context)
    {
        context.PushInstruction(Instruction.LoadBool(context.StackPosition, (ushort)(node.Value ? 1 : 0), 0), node.Position, true);
        return true;
    }

    public bool VisitNumericLiteralNode(NumericLiteralNode node, ScopeCompilationContext context)
    {
        var index = context.Function.GetConstantIndex(node.Value);
        context.PushInstruction(Instruction.LoadK(context.StackPosition, index), node.Position, true);
        return true;
    }

    public bool VisitStringLiteralNode(StringLiteralNode node, ScopeCompilationContext context)
    {
        var str = node.IsShortLiteral
            ? StringHelper.FromStringLiteral(node.Text.Span)
            : node.Text.ToString();

        var index = context.Function.GetConstantIndex(str);
        context.PushInstruction(Instruction.LoadK(context.StackPosition, index), node.Position, true);
        return true;
    }

    // identifier
    public bool VisitIdentifierNode(IdentifierNode node, ScopeCompilationContext context)
    {
        LoadIdentifier(node.Name, context, node.Position, false);
        return true;
    }

    // vararg
    public bool VisitVariableArgumentsExpressionNode(VariableArgumentsExpressionNode node, ScopeCompilationContext context)
    {
        CompileVariableArgumentsExpression(node, context, 1);
        return true;
    }

    void CompileVariableArgumentsExpression(VariableArgumentsExpressionNode node, ScopeCompilationContext context, int resultCount)
    {
        context.PushInstruction(Instruction.VarArg(context.StackPosition, (ushort)(resultCount == -1 ? 0 : resultCount + 1)), node.Position, true);
    }

    // Unary/Binary expression
    public bool VisitUnaryExpressionNode(UnaryExpressionNode node, ScopeCompilationContext context)
    {
        var b = context.StackPosition;
        node.Node.Accept(this, context);

        switch (node.Operator)
        {
            case UnaryOperator.Negate:
                context.PushInstruction(Instruction.Unm(b, b), node.Position);
                break;
            case UnaryOperator.Not:
                context.PushInstruction(Instruction.Not(b, b), node.Position);
                break;
            case UnaryOperator.Length:
                context.PushInstruction(Instruction.Len(b, b), node.Position);
                break;
        }

        return true;
    }

    public bool VisitBinaryExpressionNode(BinaryExpressionNode node, ScopeCompilationContext context)
    {
        var r = context.StackPosition;
        (var b, var c) = GetBAndC(node, context);

        switch (node.OperatorType)
        {
            case BinaryOperator.Addition:
                context.PushInstruction(Instruction.Add(r, b, c), node.Position);
                break;
            case BinaryOperator.Subtraction:
                context.PushInstruction(Instruction.Sub(r, b, c), node.Position);
                break;
            case BinaryOperator.Multiplication:
                context.PushInstruction(Instruction.Mul(r, b, c), node.Position);
                break;
            case BinaryOperator.Division:
                context.PushInstruction(Instruction.Div(r, b, c), node.Position);
                break;
            case BinaryOperator.Modulo:
                context.PushInstruction(Instruction.Mod(r, b, c), node.Position);
                break;
            case BinaryOperator.Exponentiation:
                context.PushInstruction(Instruction.Pow(r, b, c), node.Position);
                break;
            case BinaryOperator.Equality:
                context.PushInstruction(Instruction.Eq(1, b, c), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 1, 1), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 0, 0), node.Position);
                break;
            case BinaryOperator.Inequality:
                context.PushInstruction(Instruction.Eq(0, b, c), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 1, 1), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 0, 0), node.Position);
                break;
            case BinaryOperator.GreaterThan:
                context.PushInstruction(Instruction.Le(0, b, c), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 1, 1), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 0, 0), node.Position);
                break;
            case BinaryOperator.GreaterThanOrEqual:
                context.PushInstruction(Instruction.Lt(0, b, c), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 1, 1), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 0, 0), node.Position);
                break;
            case BinaryOperator.LessThan:
                context.PushInstruction(Instruction.Lt(1, b, c), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 1, 1), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 0, 0), node.Position);
                break;
            case BinaryOperator.LessThanOrEqual:
                context.PushInstruction(Instruction.Le(1, b, c), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 1, 1), node.Position);
                context.PushInstruction(Instruction.LoadBool(r, 0, 0), node.Position);
                break;
            case BinaryOperator.Concat:
                context.PushInstruction(Instruction.Concat(r, b, c), node.Position);
                break;
            case BinaryOperator.And:
                context.PushInstruction(Instruction.TestSet(r, b, 0), node.Position);
                context.PushInstruction(Instruction.Jmp(0, 1), node.Position);
                context.PushInstruction(Instruction.Move(r, c), node.Position);
                break;
            case BinaryOperator.Or:
                context.PushInstruction(Instruction.TestSet(r, b, 1), node.Position);
                context.PushInstruction(Instruction.Jmp(0, 1), node.Position);
                context.PushInstruction(Instruction.Move(r, c), node.Position);
                break;
        }

        context.StackPosition = (byte)(r + 1);

        return true;
    }

    public bool VisitGroupedExpressionNode(GroupedExpressionNode node, ScopeCompilationContext context)
    {
        return node.Expression.Accept(this, context);
    }

    // table
    public bool VisitTableConstructorExpressionNode(TableConstructorExpressionNode node, ScopeCompilationContext context)
    {
        var tableRegisterIndex = context.StackPosition;
        var newTableInstructionIndex = context.Function.Instructions.Length;
        context.PushInstruction(Instruction.NewTable(tableRegisterIndex, 0, 0), node.Position, true);

        var currentArrayChunkSize = 0;
        ushort hashMapSize = 0;
        ushort arrayBlock = 1;

        ListTableConstructorField? lastField = null;
        if (node.Fields.LastOrDefault() is ListTableConstructorField t)
        {
            lastField = t;
        }

        foreach (var group in node.Fields.GroupConsecutiveBy(x => x.GetType()))
        {
            foreach (var field in group)
            {
                var p = context.StackPosition;

                switch (field)
                {
                    case ListTableConstructorField listItem:
                        context.StackPosition = (byte)(p + currentArrayChunkSize - 50 * (arrayBlock - 1));

                        // For the last element, we need to take into account variable arguments and multiple return values.
                        if (listItem == lastField)
                        {
                            switch (listItem.Expression)
                            {
                                case CallFunctionExpressionNode call:
                                    CompileCallFunctionExpression(call, context, false, -1);
                                    break;
                                case CallTableMethodExpressionNode method:
                                    CompileTableMethod(method, context, false, -1);
                                    break;
                                case VariableArgumentsExpressionNode varArg:
                                    CompileVariableArgumentsExpression(varArg, context, -1);
                                    break;
                                default:
                                    listItem.Expression.Accept(this, context);
                                    break;
                            }

                            context.PushInstruction(Instruction.SetList(tableRegisterIndex, 0, arrayBlock), listItem.Position);
                            currentArrayChunkSize = 0;
                        }
                        else
                        {
                            listItem.Expression.Accept(this, context);

                            currentArrayChunkSize++;

                            if (currentArrayChunkSize == 50)
                            {
                                context.PushInstruction(Instruction.SetList(tableRegisterIndex, 50, arrayBlock), listItem.Position);
                                currentArrayChunkSize = 0;
                                arrayBlock++;
                            }
                        }

                        break;
                    case RecordTableConstructorField recordItem:
                        recordItem.ValueExpression.Accept(this, context);
                        var keyConstIndex = context.Function.GetConstantIndex(recordItem.Key) + 256;

                        context.PushInstruction(Instruction.SetTable(tableRegisterIndex, (ushort)keyConstIndex, p), recordItem.Position);
                        hashMapSize++;
                        break;
                    case GeneralTableConstructorField generalItem:
                        var keyIndex = context.StackPosition;
                        generalItem.KeyExpression.Accept(this, context);
                        var valueIndex = context.StackPosition;
                        generalItem.ValueExpression.Accept(this, context);

                        context.PushInstruction(Instruction.SetTable(tableRegisterIndex, keyIndex, valueIndex), generalItem.Position);
                        hashMapSize++;
                        break;
                    default:
                        throw new NotSupportedException();
                }

                context.StackPosition = p;
            }

            if (currentArrayChunkSize > 0)
            {
                context.PushInstruction(Instruction.SetList(tableRegisterIndex, (ushort)currentArrayChunkSize, arrayBlock), node.Position);
                currentArrayChunkSize = 0;
                arrayBlock = 1;
            }
        }

        context.Function.Instructions[newTableInstructionIndex].B = (ushort)(currentArrayChunkSize + (arrayBlock - 1) * 50);
        context.Function.Instructions[newTableInstructionIndex].C = hashMapSize;

        return true;
    }

    public bool VisitTableIndexerAccessExpressionNode(TableIndexerAccessExpressionNode node, ScopeCompilationContext context)
    {
        // load table
        var tablePosition = context.StackPosition;
        node.TableNode.Accept(this, context);

        // load key
        node.KeyNode.Accept(this, context);

        // push interuction
        context.PushInstruction(Instruction.GetTable(tablePosition, tablePosition, (ushort)(context.StackPosition - 1)), node.Position);
        context.StackPosition = (byte)(tablePosition + 1);

        return true;
    }

    public bool VisitTableMemberAccessExpressionNode(TableMemberAccessExpressionNode node, ScopeCompilationContext context)
    {
        // load table
        var tablePosition = context.StackPosition;
        node.TableNode.Accept(this, context);

        // load key
        var keyIndex = context.Function.GetConstantIndex(node.MemberName) + 256;

        // push interuction
        context.PushInstruction(Instruction.GetTable(tablePosition, tablePosition, (ushort)keyIndex), node.Position);
        context.StackPosition = (byte)(tablePosition + 1);

        return true;
    }

    public bool VisitCallTableMethodExpressionNode(CallTableMethodExpressionNode node, ScopeCompilationContext context)
    {
        CompileTableMethod(node, context, false, 1);
        return true;
    }

    public bool VisitCallTableMethodStatementNode(CallTableMethodStatementNode node, ScopeCompilationContext context)
    {
        CompileTableMethod(node.Expression, context, false, 0);
        return true;
    }

    void CompileTableMethod(CallTableMethodExpressionNode node, ScopeCompilationContext context, bool isTailCall, int resultCount)
    {
        // load table
        var tablePosition = context.StackPosition;
        node.TableNode.Accept(this, context);

        // load key
        var keyIndex = context.Function.GetConstantIndex(node.MethodName) + 256;

        // get closure
        context.PushInstruction(Instruction.Self(tablePosition, tablePosition, (ushort)keyIndex), node.Position);
        context.StackPosition = (byte)(tablePosition + 2);

        // load arguments
        var b = node.ArgumentNodes.Length + 2;
        if (node.ArgumentNodes.Length > 0 && !IsFixedNumberOfReturnValues(node.ArgumentNodes[^1]))
        {
            b = 0;
        }

        CompileExpressionList(node, node.ArgumentNodes, b - 1, context);

        // push call interuction
        if (isTailCall)
        {
            context.PushInstruction(Instruction.TailCall(tablePosition, (ushort)b, 0), node.Position);
            context.StackPosition = tablePosition;
        }
        else
        {
            context.PushInstruction(Instruction.Call(tablePosition, (ushort)b, (ushort)(resultCount < 0 ? 0 : resultCount + 1)), node.Position);
            context.StackPosition = (byte)(tablePosition + resultCount);
        }
    }

    // return
    public bool VisitReturnStatementNode(ReturnStatementNode node, ScopeCompilationContext context)
    {
        ushort b;

        // tail call
        if (node.Nodes.Length == 1)
        {
            var lastNode = node.Nodes[^1];

            if (lastNode is CallFunctionExpressionNode call)
            {
                CompileCallFunctionExpression(call, context, true, -1);
                return true;
            }
            else if (lastNode is CallTableMethodExpressionNode callMethod)
            {
                CompileTableMethod(callMethod, context, true, -1);
                return true;
            }
        }

        b = node.Nodes.Length > 0 && !IsFixedNumberOfReturnValues(node.Nodes[^1])
            ? (ushort)0
            : (ushort)(node.Nodes.Length + 1);

        var a = context.StackPosition;

        CompileExpressionList(node, node.Nodes, b - 1, context);

        context.PushInstruction(Instruction.Return(a, b), node.Position);

        return true;
    }

    // assignment
    public bool VisitLocalAssignmentStatementNode(LocalAssignmentStatementNode node, ScopeCompilationContext context)
    {
        var startPosition = context.StackPosition;
        CompileExpressionList(node, node.RightNodes, node.LeftNodes.Length, context);

        for (int i = 0; i < node.Identifiers.Length; i++)
        {
            context.StackPosition = (byte)(startPosition + i + 1);

            var identifier = node.Identifiers[i];

            if (context.TryGetLocalVariableInThisScope(identifier.Name, out var variable))
            {
                // assign local variable
                context.PushInstruction(Instruction.Move(variable.RegisterIndex, (ushort)(context.StackPosition - 1)), node.Position, true);
            }
            else
            {
                // register local variable
                context.AddLocalVariable(identifier.Name, new()
                {
                    RegisterIndex = (byte)(context.StackPosition - 1),
                });
            }
        }
        return true;
    }

    public bool VisitAssignmentStatementNode(AssignmentStatementNode node, ScopeCompilationContext context)
    {
        var startPosition = context.StackPosition;

        CompileExpressionList(node, node.RightNodes, node.LeftNodes.Length, context);

        for (int i = 0; i < node.LeftNodes.Length; i++)
        {
            context.StackPosition = (byte)(startPosition + i + 1);
            var leftNode = node.LeftNodes[i];

            switch (leftNode)
            {
                case IdentifierNode identifier:
                    {
                        if (context.TryGetLocalVariable(identifier.Name, out var variable))
                        {
                            // assign local variable
                            context.PushInstruction(Instruction.Move(variable.RegisterIndex, (ushort)(context.StackPosition - 1)), node.Position, true);
                        }
                        else if (context.Function.TryGetUpValue(identifier.Name, out var upValue))
                        {
                            // assign upvalue
                            context.PushInstruction(Instruction.SetUpVal((byte)(context.StackPosition - 1), (ushort)upValue.Id), node.Position);
                        }
                        else if (context.TryGetLocalVariable("_ENV".AsMemory(), out variable))
                        {
                            // assign env element
                            var index = context.Function.GetConstantIndex(identifier.Name.ToString()) + 256;
                            context.PushInstruction(Instruction.SetTable(variable.RegisterIndex, (ushort)index, (ushort)(context.StackPosition - 1)), node.Position);
                        }
                        else
                        {
                            // assign global variable
                            var index = context.Function.GetConstantIndex(identifier.Name.ToString()) + 256;
                            context.PushInstruction(Instruction.SetTabUp(0, (ushort)index, (ushort)(context.StackPosition - 1)), node.Position);
                        }
                    }
                    break;
                case TableIndexerAccessExpressionNode tableIndexer:
                    {
                        var valueIndex = context.StackPosition - 1;
                        tableIndexer.TableNode.Accept(this, context);
                        var tableIndex = context.StackPosition - 1;
                        tableIndexer.KeyNode.Accept(this, context);
                        var keyIndex = context.StackPosition - 1;
                        context.PushInstruction(Instruction.SetTable((byte)tableIndex, (ushort)keyIndex, (ushort)valueIndex), node.Position);
                    }
                    break;
                case TableMemberAccessExpressionNode tableMember:
                    {
                        var valueIndex = context.StackPosition - 1;
                        tableMember.TableNode.Accept(this, context);
                        var tableIndex = context.StackPosition - 1;
                        var keyIndex = context.Function.GetConstantIndex(tableMember.MemberName) + 256;
                        context.PushInstruction(Instruction.SetTable((byte)tableIndex, (ushort)keyIndex, (ushort)valueIndex), node.Position);
                    }
                    break;
                default:
                    throw new LuaParseException(default, default, "An error occurred while parsing the code"); // TODO: add message
            }
        }

        context.StackPosition = startPosition;

        return true;
    }

    // function call
    public bool VisitCallFunctionStatementNode(CallFunctionStatementNode node, ScopeCompilationContext context)
    {
        CompileCallFunctionExpression(node.Expression, context, false, 0);
        return true;
    }

    public bool VisitCallFunctionExpressionNode(CallFunctionExpressionNode node, ScopeCompilationContext context)
    {
        CompileCallFunctionExpression(node, context, false, 1);
        return true;
    }

    void CompileCallFunctionExpression(CallFunctionExpressionNode node, ScopeCompilationContext context, bool isTailCall, int resultCount)
    {
        // get closure
        var r = context.StackPosition;
        node.FunctionNode.Accept(this, context);

        // load arguments
        var b = node.ArgumentNodes.Length + 1;
        if (node.ArgumentNodes.Length > 0 && !IsFixedNumberOfReturnValues(node.ArgumentNodes[^1]))
        {
            b = 0;
        }

        CompileExpressionList(node, node.ArgumentNodes, b - 1, context);

        // push call interuction
        if (isTailCall)
        {
            context.PushInstruction(Instruction.TailCall(r, (ushort)b, 0), node.Position);
            context.StackPosition = r;
        }
        else
        {
            context.PushInstruction(Instruction.Call(r, (ushort)b, (ushort)(resultCount == -1 ? 0 : resultCount + 1)), node.Position);
            context.StackPosition = (byte)(r + resultCount);
        }
    }

    // function declaration
    public bool VisitFunctionDeclarationExpressionNode(FunctionDeclarationExpressionNode node, ScopeCompilationContext context)
    {
        var funcIndex = CompileFunctionProto(ReadOnlyMemory<char>.Empty, context, node.ParameterNodes, node.Nodes, node.ParameterNodes.Length, node.HasVariableArguments, false);

        // push closure instruction
        context.PushInstruction(Instruction.Closure(context.StackPosition, funcIndex), node.Position, true);

        return true;
    }

    public bool VisitLocalFunctionDeclarationStatementNode(LocalFunctionDeclarationStatementNode node, ScopeCompilationContext context)
    {
        // assign local variable
        context.AddLocalVariable(node.Name, new()
        {
            RegisterIndex = context.StackPosition,
        });

        // compile function
        var funcIndex = CompileFunctionProto(node.Name, context, node.ParameterNodes, node.Nodes, node.ParameterNodes.Length, node.HasVariableArguments, false);

        // push closure instruction
        context.PushInstruction(Instruction.Closure(context.StackPosition, funcIndex), node.Position, true);

        return true;
    }

    public bool VisitFunctionDeclarationStatementNode(FunctionDeclarationStatementNode node, ScopeCompilationContext context)
    {
        var funcIndex = CompileFunctionProto(node.Name, context, node.ParameterNodes, node.Nodes, node.ParameterNodes.Length, node.HasVariableArguments, false);

        // add closure
        var index = context.Function.GetConstantIndex(node.Name.ToString());

        // push closure instruction
        context.PushInstruction(Instruction.Closure(context.StackPosition, funcIndex), node.Position, true);

        // assign global variable
        context.PushInstruction(Instruction.SetTabUp(0, (ushort)(index + 256), (ushort)(context.StackPosition - 1)), node.Position);

        return true;
    }

    public bool VisitTableMethodDeclarationStatementNode(TableMethodDeclarationStatementNode node, ScopeCompilationContext context)
    {
        var funcIdentifier = node.MemberPath[^1];
        var funcIndex = CompileFunctionProto(funcIdentifier.Name, context, node.ParameterNodes, node.Nodes, node.ParameterNodes.Length + 1, node.HasVariableArguments, node.HasSelfParameter);

        // add closure
        var index = context.Function.GetConstantIndex(funcIdentifier.Name.ToString());

        var r = context.StackPosition;

        // assign global variable
        var first = node.MemberPath[0];
        var tableIndex = LoadIdentifier(first.Name, context, first.Position, true);
        
        for (int i = 1; i < node.MemberPath.Length - 1; i++)
        {
            var member = node.MemberPath[i];
            var constant = context.Function.GetConstantIndex(member.Name.ToString());
            context.PushInstruction(Instruction.GetTable(context.StackPosition, tableIndex, (ushort)(constant + 256)), member.Position, true);
            tableIndex = context.StackTopPosition;
        }

        // push closure instruction
        var closureIndex = context.StackPosition;
        context.PushInstruction(Instruction.Closure(closureIndex, funcIndex), node.Position, true);

        // set table
        context.PushInstruction(Instruction.SetTable(tableIndex, (ushort)(index + 256), closureIndex), funcIdentifier.Position);

        context.StackPosition = r;
        return true;
    }

    int CompileFunctionProto(ReadOnlyMemory<char> functionName, ScopeCompilationContext context, IdentifierNode[] parameters, SyntaxNode[] statements, int parameterCount, bool hasVarArg, bool hasSelfParameter)
    {
        using var funcContext = context.CreateChildFunction();
        funcContext.ChunkName = functionName.ToString();
        funcContext.ParameterCount = parameterCount;
        funcContext.HasVariableArguments = hasVarArg;

        if (hasSelfParameter)
        {
            funcContext.Scope.AddLocalVariable("self".AsMemory(), new()
            {
                RegisterIndex = 0,
            });

            funcContext.Scope.StackPosition++;
        }

        // add arguments
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            funcContext.Scope.AddLocalVariable(parameter.Name, new()
            {
                RegisterIndex = (byte)(i + (hasSelfParameter ? 1 : 0)),
            });

            funcContext.Scope.StackPosition++;
        }

        foreach (var statement in statements)
        {
            statement.Accept(this, funcContext.Scope);
        }

        // compile function
        var chunk = funcContext.ToChunk();

        int index;
        if (functionName.Length == 0)
        {
            // anonymous function
            context.Function.AddFunctionProto(chunk, out index);
        }
        else
        {
            context.Function.AddOrSetFunctionProto(functionName, chunk, out index);
        }

        return index;
    }

    // control statements
    public bool VisitDoStatementNode(DoStatementNode node, ScopeCompilationContext context)
    {
        using var scopeContext = context.CreateChildScope();

        foreach (var childNode in node.StatementNodes)
        {
            childNode.Accept(this, scopeContext);
        }

        scopeContext.TryPushCloseUpValue(node.Position);

        return true;
    }

    public bool VisitBreakStatementNode(BreakStatementNode node, ScopeCompilationContext context)
    {
        context.Function.AddUnresolvedBreak(new()
        {
            Index = context.Function.Instructions.Length
        }, node.Position);
        context.PushInstruction(Instruction.Jmp(0, 0), node.Position);

        return true;
    }

    public bool VisitIfStatementNode(IfStatementNode node, ScopeCompilationContext context)
    {
        using var endJumpIndexList = new PooledList<int>(8);
        var hasElse = node.ElseNodes.Length > 0;

        // if
        using (var scopeContext = context.CreateChildScope())
        {
            CompileConditionNode(node.IfNode.ConditionNode, scopeContext, true);

            var ifPosition = scopeContext.Function.Instructions.Length;
            scopeContext.PushInstruction(Instruction.Jmp(0, 0), node.Position);

            foreach (var childNode in node.IfNode.ThenNodes)
            {
                childNode.Accept(this, scopeContext);
            }

            if (hasElse)
            {
                endJumpIndexList.Add(scopeContext.Function.Instructions.Length);
                var a = scopeContext.HasCapturedLocalVariables ? scopeContext.StackTopPosition : (byte)0;
                scopeContext.PushInstruction(Instruction.Jmp(a, 0), node.Position, true);
            }
            else
            {
                scopeContext.TryPushCloseUpValue(node.Position);
            }

            scopeContext.Function.Instructions[ifPosition].SBx = scopeContext.Function.Instructions.Length - 1 - ifPosition;
        }

        // elseif
        foreach (var elseIf in node.ElseIfNodes)
        {
            using var scopeContext = context.CreateChildScope();

            CompileConditionNode(elseIf.ConditionNode, scopeContext, true);

            var elseifPosition = scopeContext.Function.Instructions.Length;
            scopeContext.PushInstruction(Instruction.Jmp(0, 0), node.Position);

            foreach (var childNode in elseIf.ThenNodes)
            {
                childNode.Accept(this, scopeContext);
            }

            // skip if node doesn't have else statements
            if (hasElse)
            {
                endJumpIndexList.Add(scopeContext.Function.Instructions.Length);
                var a = scopeContext.HasCapturedLocalVariables ? scopeContext.StackTopPosition : (byte)0;
                scopeContext.PushInstruction(Instruction.Jmp(a, 0), node.Position);
            }
            else
            {
                scopeContext.TryPushCloseUpValue(node.Position);
            }

            scopeContext.Function.Instructions[elseifPosition].SBx = scopeContext.Function.Instructions.Length - 1 - elseifPosition;
        }

        // else nodes
        using (var scopeContext = context.CreateChildScope())
        {
            foreach (var childNode in node.ElseNodes)
            {
                childNode.Accept(this, scopeContext);
            }

            scopeContext.TryPushCloseUpValue(node.Position);
        }

        // set JMP sBx
        foreach (var index in endJumpIndexList.AsSpan())
        {
            context.Function.Instructions[index].SBx = context.Function.Instructions.Length - 1 - index;
        }

        return true;
    }

    public bool VisitRepeatStatementNode(RepeatStatementNode node, ScopeCompilationContext context)
    {
        var startIndex = context.Function.Instructions.Length;

        context.Function.LoopLevel++;

        using var scopeContext = context.CreateChildScope();

        foreach (var childNode in node.Nodes)
        {
            childNode.Accept(this, scopeContext);
        }

        CompileConditionNode(node.ConditionNode, scopeContext, true);
        var a = scopeContext.HasCapturedLocalVariables ? scopeContext.StackTopPosition : (byte)0;
        scopeContext.PushInstruction(Instruction.Jmp(a, startIndex - scopeContext.Function.Instructions.Length - 1), node.Position);
        scopeContext.TryPushCloseUpValue(node.Position);

        context.Function.LoopLevel--;

        // resolve break statements inside repeat block
        context.Function.ResolveAllBreaks(context.StackPosition, context.Function.Instructions.Length - 1, scopeContext);

        return true;
    }

    public bool VisitWhileStatementNode(WhileStatementNode node, ScopeCompilationContext context)
    {
        var conditionIndex = context.Function.Instructions.Length;
        context.PushInstruction(Instruction.Jmp(0, 0), node.Position);

        context.Function.LoopLevel++;

        using var scopeContext = context.CreateChildScope();

        foreach (var childNode in node.Nodes)
        {
            childNode.Accept(this, scopeContext);
        }

        context.Function.LoopLevel--;

        // set JMP sBx
        context.Function.Instructions[conditionIndex].SBx = context.Function.Instructions.Length - 1 - conditionIndex;

        CompileConditionNode(node.ConditionNode, context, false);
        var a = scopeContext.HasCapturedLocalVariables ? scopeContext.StackTopPosition : (byte)0;
        context.PushInstruction(Instruction.Jmp(a, conditionIndex - context.Function.Instructions.Length), node.Position);

        // resolve break statements inside while block
        context.Function.ResolveAllBreaks(context.StackPosition, context.Function.Instructions.Length - 1, scopeContext);

        return true;
    }

    public bool VisitNumericForStatementNode(NumericForStatementNode node, ScopeCompilationContext context)
    {
        var startPosition = context.StackPosition;

        node.InitNode.Accept(this, context);
        node.LimitNode.Accept(this, context);
        if (node.StepNode != null)
        {
            node.StepNode.Accept(this, context);
        }
        else
        {
            var index = context.Function.GetConstantIndex(1);
            context.PushInstruction(Instruction.LoadK(context.StackPosition, index), node.Position, true);
        }

        var prepIndex = context.Function.Instructions.Length;
        context.PushInstruction(Instruction.ForPrep(startPosition, 0), node.Position, true);

        // compile statements
        context.Function.LoopLevel++;
        using var scopeContext = context.CreateChildScope();
        {
            // add local variable
            scopeContext.AddLocalVariable(node.VariableName, new()
            {
                RegisterIndex = startPosition
            });

            foreach (var childNode in node.StatementNodes)
            {
                childNode.Accept(this, scopeContext);
            }

            scopeContext.TryPushCloseUpValue(node.Position);
        }
        context.Function.LoopLevel--;

        // set ForPrep
        context.Function.Instructions[prepIndex].SBx = context.Function.Instructions.Length - prepIndex - 1;

        // push ForLoop
        context.PushInstruction(Instruction.ForLoop(startPosition, prepIndex - context.Function.Instructions.Length), node.Position);

        context.Function.ResolveAllBreaks(startPosition, context.Function.Instructions.Length - 1, scopeContext);

        context.StackPosition = startPosition;

        return true;
    }

    public bool VisitGenericForStatementNode(GenericForStatementNode node, ScopeCompilationContext context)
    {
        // get iterator
        var startPosition = context.StackPosition;
        if (node.ExpressionNode is CallFunctionExpressionNode call)
        {
            CompileCallFunctionExpression(call, context, false, 3);
        }
        else if (node.ExpressionNode is CallTableMethodExpressionNode method)
        {
            CompileTableMethod(method, context, false, 3);
        }
        else if (node.ExpressionNode is VariableArgumentsExpressionNode varArg)
        {
            CompileVariableArgumentsExpression(varArg, context, 3);
        }
        else
        {
            node.ExpressionNode.Accept(this, context);
        }

        // jump to TFORCALL
        var startJumpIndex = context.Function.Instructions.Length;
        context.PushInstruction(Instruction.Jmp(0, 0), node.Position);

        // compile statements
        context.Function.LoopLevel++;
        using var scopeContext = context.CreateChildScope();
        {
            scopeContext.StackPosition = (byte)(startPosition + 3 + node.Names.Length);

            // add local variables
            for (int i = 0; i < node.Names.Length; i++)
            {
                var name = node.Names[i];
                scopeContext.AddLocalVariable(name.Name, new()
                {
                    RegisterIndex = (byte)(startPosition + 3 + i)
                });
            }

            foreach (var childNode in node.StatementNodes)
            {
                childNode.Accept(this, scopeContext);
            }

            scopeContext.TryPushCloseUpValue(node.Position);
        }
        context.Function.LoopLevel--;

        // set jump
        context.Function.Instructions[startJumpIndex].SBx = context.Function.Instructions.Length - startJumpIndex - 1;

        // push OP_TFORCALL and OP_TFORLOOP
        context.PushInstruction(Instruction.TForCall(startPosition, (ushort)node.Names.Length), node.Position);
        context.PushInstruction(Instruction.TForLoop((byte)(startPosition + 2), startJumpIndex - context.Function.Instructions.Length), node.Position);

        context.Function.ResolveAllBreaks(startPosition, context.Function.Instructions.Length - 1, scopeContext);
        context.StackPosition = startPosition;

        return true;
    }

    public bool VisitLabelStatementNode(LabelStatementNode node, ScopeCompilationContext context)
    {
        var desc = new LabelDescription()
        {
            Name = node.Name,
            Index = context.Function.Instructions.Length,
            RegisterIndex = context.StackPosition
        };

        context.AddLabel(desc);
        context.Function.ResolveGoto(desc);

        return true;
    }

    public bool VisitGotoStatementNode(GotoStatementNode node, ScopeCompilationContext context)
    {
        if (context.TryGetLabel(node.Name, out var description))
        {
            context.PushInstruction(Instruction.Jmp(description.RegisterIndex, description.Index - context.Function.Instructions.Length - 1), node.Position);
        }
        else
        {
            context.Function.AddUnresolvedGoto(new()
            {
                Name = node.Name,
                JumpInstructionIndex = context.Function.Instructions.Length
            });

            // add uninitialized jmp instruction
            context.PushInstruction(Instruction.Jmp(0, 0), node.Position);
        }

        return true;
    }

    static byte LoadIdentifier(ReadOnlyMemory<char> name, ScopeCompilationContext context, SourcePosition sourcePosition, bool dontLoadLocalVariable)
    {
        var p = context.StackPosition;

        if (context.TryGetLocalVariable(name, out var variable))
        {
            if (dontLoadLocalVariable)
            {
                return variable.RegisterIndex;
            }
            else if (p == variable.RegisterIndex)
            {
                context.StackPosition++;
                return p;
            }
            else
            {
                context.PushInstruction(Instruction.Move(p, variable.RegisterIndex), sourcePosition, true);
                return p;
            }
        }
        else if (context.Function.TryGetUpValue(name, out var upValue))
        {
            context.PushInstruction(Instruction.GetUpVal(p, (ushort)upValue.Id), sourcePosition, true);
            return p;
        }
        else if (context.TryGetLocalVariable("_ENV".AsMemory(), out variable))
        {
            var keyStringIndex = context.Function.GetConstantIndex(name.ToString()) + 256;
            context.PushInstruction(Instruction.GetTable(p, variable.RegisterIndex, (ushort)keyStringIndex), sourcePosition, true);
            return p;
        }
        else
        {
            context.Function.TryGetUpValue("_ENV".AsMemory(), out upValue);
            var index = context.Function.GetConstantIndex(name.ToString()) + 256;
            context.PushInstruction(Instruction.GetTabUp(p, (ushort)upValue.Id, (ushort)index), sourcePosition, true);
            return p;
        }
    }

    static bool IsFixedNumberOfReturnValues(ExpressionNode node)
    {
        return node is not (CallFunctionExpressionNode or CallTableMethodExpressionNode or VariableArgumentsExpressionNode);
    }

    /// <summary>
    /// Compiles a conditional boolean branch: if true (or false), the next instruction added is skipped.
    /// </summary>
    /// <param name="node">Condition node</param>
    /// <param name="context">Context</param>
    /// <param name="falseIsSkip">If true, generates an instruction sequence that skips the next instruction if the condition is false.</param>
    void CompileConditionNode(ExpressionNode node, ScopeCompilationContext context, bool falseIsSkip)
    {
        if (node is BinaryExpressionNode binaryExpression)
        {
            switch (binaryExpression.OperatorType)
            {
                case BinaryOperator.Equality:
                    {
                        (var b, var c) = GetBAndC(binaryExpression, context);
                        context.PushInstruction(Instruction.Eq(falseIsSkip ? (byte)0 : (byte)1, b, c), node.Position);
                        return;
                    }
                case BinaryOperator.Inequality:
                    {
                        (var b, var c) = GetBAndC(binaryExpression, context);
                        context.PushInstruction(Instruction.Eq(falseIsSkip ? (byte)1 : (byte)0, b, c), node.Position);
                        return;
                    }
                case BinaryOperator.LessThan:
                    {
                        (var b, var c) = GetBAndC(binaryExpression, context);
                        context.PushInstruction(Instruction.Lt(falseIsSkip ? (byte)0 : (byte)1, b, c), node.Position);
                        return;
                    }
                case BinaryOperator.LessThanOrEqual:
                    {
                        (var b, var c) = GetBAndC(binaryExpression, context);
                        context.PushInstruction(Instruction.Le(falseIsSkip ? (byte)0 : (byte)1, b, c), node.Position);
                        return;
                    }
                case BinaryOperator.GreaterThan:
                    {
                        (var b, var c) = GetBAndC(binaryExpression, context);
                        context.PushInstruction(Instruction.Le(falseIsSkip ? (byte)1 : (byte)0, b, c), node.Position);
                        return;
                    }
                case BinaryOperator.GreaterThanOrEqual:
                    {
                        (var b, var c) = GetBAndC(binaryExpression, context);
                        context.PushInstruction(Instruction.Lt(falseIsSkip ? (byte)1 : (byte)0, b, c), node.Position);
                        return;
                    }
            }
        }

        node.Accept(this, context);
        context.PushInstruction(Instruction.Test((byte)(context.StackPosition - 1), falseIsSkip ? (byte)0 : (byte)1), node.Position);
    }

    void CompileExpressionList(SyntaxNode rootNode, ExpressionNode[] expressions, int minimumCount, ScopeCompilationContext context)
    {
        var isLastFunction = false;
        for (int i = 0; i < expressions.Length; i++)
        {
            var expression = expressions[i];
            var isLast = i == expressions.Length - 1;
            var resultCount = isLast ? (minimumCount == -1 ? -1 : minimumCount - i) : 1;

            if (expression is CallFunctionExpressionNode call)
            {
                CompileCallFunctionExpression(call, context, false, resultCount);
                isLastFunction = isLast;
            }
            else if (expression is CallTableMethodExpressionNode method)
            {
                CompileTableMethod(method, context, false, resultCount);
                isLastFunction = isLast;
            }
            else if (expression is VariableArgumentsExpressionNode varArg)
            {
                CompileVariableArgumentsExpression(varArg, context, resultCount);
                isLastFunction = isLast;
            }
            else
            {
                expression.Accept(this, context);
                isLastFunction = false;
            }
        }

        // fill space with nil
        var varCount = minimumCount - expressions.Length;
        if (varCount > 0 && !isLastFunction)
        {
            context.PushInstruction(Instruction.LoadNil(context.StackPosition, (ushort)varCount), rootNode.Position);
            context.StackPosition = (byte)(context.StackPosition + varCount);
        }
    }

    (byte b, byte c) GetBAndC(BinaryExpressionNode node, ScopeCompilationContext context)
    {
        byte b, c;
        if (node.LeftNode is IdentifierNode leftIdentifier)
        {
            b = LoadIdentifier(leftIdentifier.Name, context, leftIdentifier.Position, true);
        }
        else
        {
            node.LeftNode.Accept(this, context);
            b = (byte)(context.StackPosition - 1);
        }
        if (node.RightNode is IdentifierNode rightIdentifier)
        {
            c = LoadIdentifier(rightIdentifier.Name, context, rightIdentifier.Position, true);
        }
        else
        {
            node.RightNode.Accept(this, context);
            c = (byte)(context.StackPosition - 1);
        }

        return (b, c);
    }
}