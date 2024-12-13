namespace Lox.VM.Scanner;

public sealed partial class LoxScanner
{
    #region Methods
    /// <summary>
    /// Scans the next token
    /// </summary>
    /// <returns>The scanned token</returns>
    private Token ScanToken()
    {
        return Token.MakeErrorToken("Unexpected character", this.currentLine);
    }
    #endregion
}
