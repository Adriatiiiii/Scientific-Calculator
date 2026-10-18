using System.Globalization;

namespace SiencCalculator;

public partial class MainPage : ContentPage
{
    private string _expression = string.Empty;
    private bool _isDegrees = true;
    private bool _hasCalculated;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnButtonPressed(object? sender, EventArgs e)
    {
        var button = (Button)sender!;
        var key = button.CommandParameter as string ?? button.Text;

        if (key is "DEG" or "RAD")
        {
            _isDegrees = !_isDegrees;
            AngleModeButton.Text = _isDegrees ? "DEG" : "RAD";
            UpdatePreview();
            return;
        }

        if (key == "AC")
        {
            _expression = string.Empty;
            _hasCalculated = false;
            UpdatePreview();
            return;
        }

        if (key == "=")
        {
            _expression += new string(')', Math.Max(0, _expression.Count(character => character == '(') - _expression.Count(character => character == ')')));
            Calculate();
            return;
        }

        if (key == "⌫")
        {
            if (_expression.Length > 0)
            {
                _expression = _expression[..^1];
            }

            _hasCalculated = false;
            UpdatePreview();
            return;
        }

        if (key == "±")
        {
            _expression = _expression.StartsWith("-(", StringComparison.Ordinal) && _expression.EndsWith(')')
                ? _expression[2..^1]
                : _expression.Length == 0 ? "-" : $"-({_expression})";
            _hasCalculated = false;
            UpdatePreview();
            return;
        }

        var insertion = key switch
        {
            "÷" => "/",
            "×" => "*",
            "−" => "-",
            "√" => "sqrt(",
            "sin" or "cos" or "tan" or "ln" or "log" => $"{key}(",
            "x²" => "^2",
            "xʸ" => "^",
            "π" => "pi",
            "e" => "e",
            "n!" => "!",
            "1/x" => _expression.Length == 0 ? "1/(" : "^-1",
            _ => key
        };

        if (_hasCalculated && StartsNewExpression(key))
        {
            _expression = string.Empty;
        }
        else if (_hasCalculated && IsOperator(key))
        {
            _expression = FormatValue(double.Parse(ResultLabel.Text!, CultureInfo.InvariantCulture));
        }

        _hasCalculated = false;

        if (key == "." && HasDecimalInCurrentNumber())
        {
            return;
        }

        _expression += insertion;
        UpdatePreview();
    }

    private void Calculate()
    {
        if (_expression.Length == 0)
        {
            return;
        }

        try
        {
            var result = CalculatorEngine.Evaluate(_expression, _isDegrees);
            _expression = FormatValue(result);
            _hasCalculated = true;
            ExpressionLabel.TextColor = Color.FromArgb("#8793A4");
            ExpressionLabel.Text = " ";
            ResultLabel.Text = _expression;
        }
        catch (FormatException)
        {
            ShowError("Invalid expression");
        }
        catch (ArithmeticException)
        {
            ShowError("Math error");
        }
    }

    private void UpdatePreview()
    {
        ExpressionLabel.Text = _expression.Length == 0 ? " " : _expression
            .Replace("*", " × ", StringComparison.Ordinal)
            .Replace("/", " ÷ ", StringComparison.Ordinal)
            .Replace("-", "−", StringComparison.Ordinal);
        ExpressionLabel.TextColor = Color.FromArgb("#8793A4");

        if (_expression.Length == 0)
        {
            ResultLabel.Text = "0";
            return;
        }

        try
        {
            ResultLabel.Text = FormatValue(CalculatorEngine.Evaluate(_expression, _isDegrees));
        }
        catch (FormatException)
        {
            ResultLabel.Text = "…";
        }
        catch (ArithmeticException)
        {
            ResultLabel.Text = "…";
        }
    }

    private void ShowError(string message)
    {
        ExpressionLabel.TextColor = Color.FromArgb("#FF9A9A");
        ExpressionLabel.Text = message;
        ResultLabel.Text = "—";
        _hasCalculated = false;
    }

    private bool HasDecimalInCurrentNumber()
    {
        for (var i = _expression.Length - 1; i >= 0; i--)
        {
            if (_expression[i] == '.')
            {
                return true;
            }

            if (!char.IsDigit(_expression[i]))
            {
                return false;
            }
        }

        return false;
    }

    private static bool StartsNewExpression(string key) =>
        char.IsDigit(key[0]) || key is "." or "(" or "π" or "e" or "√" or "sin" or "cos" or "tan" or "ln" or "log";

    private static bool IsOperator(string key) =>
        key is "+" or "−" or "×" or "÷" or "^" or "%" or "n!" or "1/x" or "x²";

    private static string FormatValue(double value) =>
        value.ToString("G12", CultureInfo.InvariantCulture);
}
