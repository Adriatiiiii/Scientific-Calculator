using System.Globalization;

namespace SiencCalculator;

internal sealed class CalculatorEngine
{
    private readonly string _text;
    private readonly bool _degrees;
    private int _position;

    private CalculatorEngine(string text, bool degrees)
    {
        _text = text;
        _degrees = degrees;
    }

    public static double Evaluate(string expression, bool degrees)
    {
        var parser = new CalculatorEngine(expression, degrees);
        var result = parser.ParseExpression();
        if (parser._position != expression.Length)
        {
            throw new FormatException("Unexpected input.");
        }

        return EnsureFinite(result);
    }

    private double ParseExpression()
    {
        var value = ParseTerm();
        while (true)
        {
            if (Match('+'))
            {
                value += ParseTerm();
            }
            else if (Match('-'))
            {
                value -= ParseTerm();
            }
            else
            {
                return EnsureFinite(value);
            }
        }
    }

    private double ParseTerm()
    {
        var value = ParseUnary();
        while (true)
        {
            if (Match('*'))
            {
                value *= ParseUnary();
            }
            else if (Match('/'))
            {
                var divisor = ParseUnary();
                if (divisor == 0)
                {
                    throw new ArithmeticException("Division by zero.");
                }

                value /= divisor;
            }
            else if (StartsImplicitMultiplication())
            {
                value *= ParseUnary();
            }
            else
            {
                return EnsureFinite(value);
            }
        }
    }

    private double ParseUnary()
    {
        if (Match('+'))
        {
            return ParseUnary();
        }

        if (Match('-'))
        {
            return -ParseUnary();
        }

        return ParsePower();
    }

    private double ParsePower()
    {
        var value = ParsePostfix();
        if (Match('^'))
        {
            value = Math.Pow(value, ParseUnary());
        }

        return EnsureFinite(value);
    }

    private double ParsePostfix()
    {
        var value = ParsePrimary();
        while (true)
        {
            if (Match('%'))
            {
                value /= 100;
            }
            else if (Match('!'))
            {
                value = Factorial(value);
            }
            else
            {
                return EnsureFinite(value);
            }
        }
    }

    private double ParsePrimary()
    {
        if (Match('('))
        {
            var value = ParseExpression();
            Expect(')');
            return value;
        }

        if (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.'))
        {
            return ParseNumber();
        }

        var identifier = ParseIdentifier();
        return identifier switch
        {
            "pi" => Math.PI,
            "e" => Math.E,
            "sin" => ApplyTrig(Math.Sin),
            "cos" => ApplyTrig(Math.Cos),
            "tan" => ApplyTrig(Math.Tan),
            "sqrt" => SquareRoot(),
            "ln" => Logarithm(),
            "log" => Logarithm(base10: true),
            _ => throw new FormatException("Unknown function.")
        };
    }

    private double ApplyTrig(Func<double, double> function)
    {
        Expect('(');
        var angle = ParseExpression();
        Expect(')');
        var radians = _degrees ? angle * Math.PI / 180 : angle;
        if (function == Math.Tan && Math.Abs(Math.Cos(radians)) < 1e-14)
        {
            throw new ArithmeticException("Tangent is undefined for this angle.");
        }

        return EnsureFinite(function(radians));
    }

    private double SquareRoot()
    {
        Expect('(');
        var value = ParseExpression();
        Expect(')');
        if (value < 0)
        {
            throw new ArithmeticException("Square root of a negative number.");
        }

        return Math.Sqrt(value);
    }

    private double Logarithm(bool base10 = false)
    {
        Expect('(');
        var value = ParseExpression();
        Expect(')');
        if (value <= 0)
        {
            throw new ArithmeticException("Logarithm requires a positive number.");
        }

        return base10 ? Math.Log10(value) : Math.Log(value);
    }

    private double ParseNumber()
    {
        var start = _position;
        var decimalPointSeen = false;
        while (_position < _text.Length)
        {
            var current = _text[_position];
            if (char.IsDigit(current))
            {
                _position++;
            }
            else if (current == '.' && !decimalPointSeen)
            {
                decimalPointSeen = true;
                _position++;
            }
            else
            {
                break;
            }
        }

        if (!double.TryParse(_text.AsSpan(start, _position - start), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("Invalid number.");
        }

        return value;
    }

    private string ParseIdentifier()
    {
        var start = _position;
        while (_position < _text.Length && char.IsLetter(_text[_position]))
        {
            _position++;
        }

        if (start == _position)
        {
            throw new FormatException("Expected a number.");
        }

        return _text[start.._position];
    }

    private bool StartsImplicitMultiplication() =>
        _position < _text.Length &&
        (char.IsLetter(_text[_position]) || _text[_position] == '(');

    private bool Match(char expected)
    {
        if (_position < _text.Length && _text[_position] == expected)
        {
            _position++;
            return true;
        }

        return false;
    }

    private void Expect(char expected)
    {
        if (!Match(expected))
        {
            throw new FormatException($"Expected '{expected}'.");
        }
    }

    private static double Factorial(double value)
    {
        if (value < 0 || value > 170 || value != Math.Truncate(value))
        {
            throw new ArithmeticException("Factorial requires an integer from 0 to 170.");
        }

        var result = 1d;
        for (var i = 2; i <= (int)value; i++)
        {
            result *= i;
        }

        return result;
    }

    private static double EnsureFinite(double value) =>
        double.IsFinite(value) ? value : throw new ArithmeticException("Result is outside the supported range.");
}
