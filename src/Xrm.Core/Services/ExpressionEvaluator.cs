using System.Globalization;
using System.Text.Json;

namespace Xrm.Core.Services;

/// <summary>
/// Evaluates simple arithmetic expressions with field references.
/// Supports: +, -, *, /, parentheses, numeric literals, and field name identifiers.
/// </summary>
public static class ExpressionEvaluator
{
    public static double? Evaluate(string expression, Dictionary<string, JsonElement> data)
    {
        try
        {
            var tokens = Tokenize(expression);
            var pos = 0;
            var result = ParseExpression(tokens, ref pos, data);
            return result;
        }
        catch
        {
            return null;
        }
    }

    private enum TokenType { Number, Identifier, Plus, Minus, Multiply, Divide, LParen, RParen }

    private record Token(TokenType Type, string Value);

    private static List<Token> Tokenize(string expr)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < expr.Length)
        {
            if (char.IsWhiteSpace(expr[i])) { i++; continue; }

            if (expr[i] == '+') { tokens.Add(new(TokenType.Plus, "+")); i++; }
            else if (expr[i] == '-')
            {
                // Unary minus: after operator, LParen, or at start
                if (tokens.Count == 0 || tokens[^1].Type is TokenType.Plus or TokenType.Minus
                    or TokenType.Multiply or TokenType.Divide or TokenType.LParen)
                {
                    // Parse as negative number or negate next token
                    i++;
                    if (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                    {
                        var start = i - 1;
                        while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.')) i++;
                        tokens.Add(new(TokenType.Number, expr[start..i]));
                    }
                    else
                    {
                        // Negate: insert 0 - ...
                        tokens.Add(new(TokenType.Number, "-1"));
                        tokens.Add(new(TokenType.Multiply, "*"));
                    }
                }
                else
                {
                    tokens.Add(new(TokenType.Minus, "-")); i++;
                }
            }
            else if (expr[i] == '*') { tokens.Add(new(TokenType.Multiply, "*")); i++; }
            else if (expr[i] == '/') { tokens.Add(new(TokenType.Divide, "/")); i++; }
            else if (expr[i] == '(') { tokens.Add(new(TokenType.LParen, "(")); i++; }
            else if (expr[i] == ')') { tokens.Add(new(TokenType.RParen, ")")); i++; }
            else if (char.IsDigit(expr[i]) || expr[i] == '.')
            {
                var start = i;
                while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.')) i++;
                tokens.Add(new(TokenType.Number, expr[start..i]));
            }
            else if (char.IsLetter(expr[i]) || expr[i] == '_')
            {
                var start = i;
                while (i < expr.Length && (char.IsLetterOrDigit(expr[i]) || expr[i] == '_')) i++;
                tokens.Add(new(TokenType.Identifier, expr[start..i]));
            }
            else { i++; } // skip unknown
        }
        return tokens;
    }

    private static double ParseExpression(List<Token> tokens, ref int pos, Dictionary<string, JsonElement> data)
    {
        var left = ParseTerm(tokens, ref pos, data);
        while (pos < tokens.Count && tokens[pos].Type is TokenType.Plus or TokenType.Minus)
        {
            var op = tokens[pos++].Type;
            var right = ParseTerm(tokens, ref pos, data);
            left = op == TokenType.Plus ? left + right : left - right;
        }
        return left;
    }

    private static double ParseTerm(List<Token> tokens, ref int pos, Dictionary<string, JsonElement> data)
    {
        var left = ParseFactor(tokens, ref pos, data);
        while (pos < tokens.Count && tokens[pos].Type is TokenType.Multiply or TokenType.Divide)
        {
            var op = tokens[pos++].Type;
            var right = ParseFactor(tokens, ref pos, data);
            left = op == TokenType.Multiply ? left * right : (right != 0 ? left / right : 0);
        }
        return left;
    }

    private static double ParseFactor(List<Token> tokens, ref int pos, Dictionary<string, JsonElement> data)
    {
        if (pos >= tokens.Count) return 0;

        var token = tokens[pos];

        if (token.Type == TokenType.LParen)
        {
            pos++;
            var result = ParseExpression(tokens, ref pos, data);
            if (pos < tokens.Count && tokens[pos].Type == TokenType.RParen) pos++;
            return result;
        }

        if (token.Type == TokenType.Number)
        {
            pos++;
            return double.Parse(token.Value, CultureInfo.InvariantCulture);
        }

        if (token.Type == TokenType.Identifier)
        {
            pos++;
            return ResolveField(token.Value, data);
        }

        pos++;
        return 0;
    }

    private static double ResolveField(string name, Dictionary<string, JsonElement> data)
    {
        if (!data.TryGetValue(name, out var el)) return 0;

        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetDouble(),
            JsonValueKind.String when double.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) => n,
            _ => 0
        };
    }
}
