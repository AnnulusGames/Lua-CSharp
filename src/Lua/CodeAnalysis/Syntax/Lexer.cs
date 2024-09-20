﻿using System.Runtime.CompilerServices;

namespace Lua.CodeAnalysis.Syntax;

public ref struct Lexer
{
    public required ReadOnlyMemory<char> Source { get; init; }
    public string? ChunkName { get; init; }
    
    SyntaxToken current;
    SourcePosition position = new(1, 0);
    int offset;

    public Lexer()
    {
    }

    public readonly SyntaxToken Current => current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Advance(int count)
    {
        var span = Source.Span;
        for (int i = 0; i < count; i++)
        {
            if (offset >= span.Length)
            {
                LuaParseException.SyntaxError(ChunkName, position, null);
            }

            var c = span[offset];
            offset++;

            var isLF = c is '\n';
            var isCR = c is '\r' && (span.Length == offset || span[offset] is not '\n');

            if (isLF || isCR)
            {
                position.Column = 0;
                position.Line++;
            }
            else
            {
                position.Column++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool TryRead(int offset, out char value)
    {
        if (Source.Length <= offset)
        {
            value = default;
            return false;
        }

        value = Source.Span[offset];
        return true;
    }

    public bool MoveNext()
    {
        if (Source.Length <= offset) return false;

        var span = Source.Span;
        var startOffset = offset;
        var position = this.position;

        var c1 = span[offset];
        Advance(1);
        var c2 = span.Length == offset ? char.MinValue : span[offset];

        switch (c1)
        {
            case ' ':
            case '\t':
                return MoveNext();
            case '\n':
                current = SyntaxToken.EndOfLine(position);
                return true;
            case '\r':
                if (c2 == '\n') Advance(1);
                current = SyntaxToken.EndOfLine(position);
                return true;
            case '(':
                current = SyntaxToken.LParen(position);
                return true;
            case ')':
                current = SyntaxToken.RParen(position);
                return true;
            case '{':
                current = SyntaxToken.LCurly(position);
                return true;
            case '}':
                current = SyntaxToken.RCurly(position);
                return true;
            case '[':
                current = SyntaxToken.LSquare(position);
                return true;
            case ']':
                current = SyntaxToken.RSquare(position);
                return true;
            case '+':
                current = SyntaxToken.Addition(position);
                return true;
            case '-':
                // comment
                if (c2 == '-')
                {
                    Advance(1);

                    // block comment
                    if (TryRead(offset, out var c3) && c3 == '[' &&
                        TryRead(offset, out var c4) && c4 == '[')
                    {
                        Advance(2);
                        ReadUntilEndOfBlockComment(ref span, ref offset);
                    }
                    else // line comment
                    {
                        ReadUntilEOL(ref span, ref offset, out _);
                    }

                    return MoveNext();
                }
                else
                {
                    current = SyntaxToken.Subtraction(position);
                    return true;
                }
            case '*':
                current = SyntaxToken.Multiplication(position);
                return true;
            case '/':
                current = SyntaxToken.Division(position);
                return true;
            case '%':
                current = SyntaxToken.Modulo(position);
                return true;
            case '^':
                current = SyntaxToken.Exponentiation(position);
                return true;
            case '=':
                if (c2 == '=')
                {
                    current = SyntaxToken.Equality(position);
                    Advance(1);
                }
                else
                {
                    current = SyntaxToken.Assignment(position);
                }
                return true;
            case '~':
                if (c2 == '=')
                {
                    current = SyntaxToken.Inequality(position);
                    Advance(1);
                }
                else
                {
                    throw new LuaParseException(ChunkName, position, $"error: Invalid '~' token");
                }
                return true;
            case '>':
                if (c2 == '=')
                {
                    current = SyntaxToken.GreaterThanOrEqual(position);
                    Advance(1);
                }
                else
                {
                    current = SyntaxToken.GreaterThan(position);
                }
                return true;
            case '<':
                if (c2 == '=')
                {
                    current = SyntaxToken.LessThanOrEqual(position);
                    Advance(1);
                }
                else
                {
                    current = SyntaxToken.LessThan(position);
                }
                return true;
            case '.':
                if (c2 == '.')
                {
                    var c3 = span.Length == (offset + 1) ? char.MinValue : span[offset + 1];

                    if (c3 == '.')
                    {
                        // vararg
                        current = SyntaxToken.VarArg(position);
                        Advance(2);
                    }
                    else
                    {
                        // concat
                        current = SyntaxToken.Concat(position);
                        Advance(1);
                    }

                    return true;
                }

                if (!IsNumeric(c2))
                {
                    current = SyntaxToken.Dot(position);
                    return true;
                }

                break;
            case '#':
                current = SyntaxToken.Length(position);
                return true;
            case ',':
                current = SyntaxToken.Comma(position);
                return true;
            case ';':
                current = SyntaxToken.SemiColon(position);
                return true;
        }

        // numeric literal
        if (IsNumeric(c1))
        {
            if (c1 is '0' && c2 is 'x' or 'X') // hex 0x
            {
                Advance(1);
                ReadDigit(ref span, ref offset, out var readCount);

                if (readCount == 0)
                {
                    throw new LuaParseException(ChunkName, this.position, $"error: Illegal hexadecimal number");
                }
            }
            else
            {
                ReadNumber(ref span, ref offset, out _);

                if (span.Length > offset)
                {
                    var c = span[offset];

                    if (c is '.')
                    {
                        Advance(1);
                        ReadNumber(ref span, ref offset, out _);
                    }
                    else if (c is 'e' or 'E')
                    {
                        Advance(1);
                        if (span[offset] is '-' or '+') Advance(1);

                        ReadNumber(ref span, ref offset, out _);
                    }
                }
            }

            current = new(SyntaxTokenType.Number, Source[startOffset..offset], position);
            return true;
        }

        // label
        if (c1 is ':')
        {
            if (c2 is ':')
            {
                var stringStartOffset = offset + 1;
                Advance(2);

                var prevC = char.MinValue;

                while (span.Length > offset)
                {
                    var c = span[offset];
                    if (prevC == ':' && c == ':') break;

                    Advance(1);
                    prevC = c;
                }

                current = SyntaxToken.Label(Source[stringStartOffset..(offset - 1)], position);
                Advance(1);
            }
            else
            {
                current = SyntaxToken.Colon(position);
            }

            return true;
        }

        // string
        if (c1 is '"' or '\'')
        {
            var quote = c1;
            var stringStartOffset = offset;
            Advance(1);

            while (span.Length > offset)
            {
                var c = span[offset];
                if (c == quote) break;

                if (c is '\n' or '\r')
                {
                    throw new LuaParseException(ChunkName, this.position, "error: Unterminated string");
                }

                // if (c is '\\')
                // {

                // }

                Advance(1);
            }

            current = SyntaxToken.String(Source[stringStartOffset..offset], position);
            Advance(1);
            return true;
        }

        // identifier
        if (IsIdentifier(c1))
        {
            while (span.Length > offset && IsIdentifier(span[offset]))
            {
                Advance(1);
            }

            var identifier = Source[startOffset..offset];

            current = identifier.Span switch
            {
                Keywords.Nil => SyntaxToken.Nil(position),
                Keywords.True => SyntaxToken.True(position),
                Keywords.False => SyntaxToken.False(position),
                Keywords.And => SyntaxToken.And(position),
                Keywords.Or => SyntaxToken.Or(position),
                Keywords.Not => SyntaxToken.Not(position),
                Keywords.End => SyntaxToken.End(position),
                Keywords.Then => SyntaxToken.Then(position),
                Keywords.If => SyntaxToken.If(position),
                Keywords.ElseIf => SyntaxToken.ElseIf(position),
                Keywords.Else => SyntaxToken.Else(position),
                Keywords.Local => SyntaxToken.Local(position),
                Keywords.Return => SyntaxToken.Return(position),
                Keywords.Goto => SyntaxToken.Goto(position),
                Keywords.Do => SyntaxToken.Do(position),
                Keywords.In => SyntaxToken.In(position),
                Keywords.While => SyntaxToken.While(position),
                Keywords.Repeat => SyntaxToken.Repeat(position),
                Keywords.For => SyntaxToken.For(position),
                Keywords.Until => SyntaxToken.Until(position),
                Keywords.Break => SyntaxToken.Break(position),
                Keywords.Function => SyntaxToken.Function(position),
                _ => new(SyntaxTokenType.Identifier, identifier, position),
            };

            return true;
        }

        throw new LuaParseException(ChunkName, position, $"unexpected symbol near '{c1}'");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadUntilEOL(ref ReadOnlySpan<char> span, ref int offset, out int readCount)
    {
        readCount = 0;
        var flag = true;
        while (flag)
        {
            if (span.Length <= offset) return;

            var c1 = span[offset];

            if (c1 is '\n')
            {
                flag = false;
            }
            else if (c1 is '\r')
            {
                var c2 = span.Length == offset + 1 ? char.MinValue : span[offset + 1];
                if (c2 is '\n')
                {
                    Advance(1);
                    readCount++;
                }
                flag = false;
            }

            Advance(1);
            readCount++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadUntilEndOfBlockComment(ref ReadOnlySpan<char> span, ref int offset)
    {
        var start = position;

        while (span.Length > offset + 1)
        {
            if (span[offset] is ']' &&
                span[offset + 1] is ']')
            {
                Advance(2);
                return;
            }

            Advance(1);
        }

        LuaParseException.UnfinishedLongComment(ChunkName, start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadDigit(ref ReadOnlySpan<char> span, ref int offset, out int readCount)
    {
        readCount = 0;
        while (span.Length > offset && IsDigit(span[offset]))
        {
            Advance(1);
            readCount++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadNumber(ref ReadOnlySpan<char> span, ref int offset, out int readCount)
    {
        readCount = 0;
        while (span.Length > offset && IsNumeric(span[offset]))
        {
            Advance(1);
            readCount++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsDigit(char c)
    {
        return IsNumeric(c) ||
            ('a' <= c && c <= 'f') ||
            ('A' <= c && c <= 'F');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsNumeric(char c)
    {
        return '0' <= c && c <= '9';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsIdentifier(char c)
    {
        return c == '_' ||
            ('A' <= c && c <= 'Z') ||
            ('a' <= c && c <= 'z') ||
            IsNumeric(c);
    }
}
