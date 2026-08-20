namespace DiceUtilsCmdPalExt.DiceLanguage.Parser;

public enum TokenType
{
    Integer,
    AddOp,
    MulOp,
    Pool, 
    Keyword,
    OpenParen,
    CloseParen,
    EOF
}

public record Token(TokenType Type, string Value);
